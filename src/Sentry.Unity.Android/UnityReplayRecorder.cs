using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Sentry.Unity.Android;

/// <summary>
/// Session replay's recorder when Unity captures the frames: the Android SDK's replay drives it through its
/// <c>Recorder</c> interface, and Unity hands it each frame as a bitmap.
/// </summary>
/// <remarks>
/// The Android SDK calls the recorder on Android's UI thread. Those calls only update the state below; Unity's main
/// thread reads it and captures the frames, and a worker thread turns them into bitmaps and hands them over. No
/// thread waits on another: closing the SDK blocks Unity's main thread while the UI thread stops and closes the
/// recorder.
/// </remarks>
internal sealed class UnityReplayRecorder
{
    private const string ReplayIntegrationClass = "io.sentry.android.replay.ReplayIntegration";
    private static readonly TimeSpan StatsInterval = TimeSpan.FromSeconds(10);

    private sealed class FrameConfig
    {
        public FrameConfig(int width, int height, int frameRate)
        {
            Width = width;
            Height = height;
            FrameRate = Math.Max(1, frameRate);
        }

        public int Width { get; }
        public int Height { get; }
        public int FrameRate { get; }
    }

    private readonly IDiagnosticLogger? _logger;
    private readonly RecorderProxy _recorder;
    private readonly RecorderProvider _provider;

    // Written on Android's UI thread.
    private volatile bool _recording;
    private volatile bool _resumed;
    private volatile bool _closed;
    private volatile FrameConfig? _config;
    // 1 when the replay needs a frame config: every (re)started replay starts without one.
    private int _configNeeded;
    // The window size of the current config. Android's UI thread only.
    private int _configuredWidth;
    private int _configuredHeight;

    // Set on Unity's main thread before the recorder starts.
    private volatile AndroidJavaObject? _replayIntegration;
    private AndroidJavaObject? _mainHandler;

    // The frame the worker hands over next: filled on Unity's main thread while no frame is queued.
    private readonly AutoResetEvent _frameQueuedEvent = new(false);
    private volatile bool _frameQueued;
    private sbyte[] _pixels = Array.Empty<sbyte>();
    private int _pixelsWidth;
    private int _pixelsHeight;
    private Thread? _worker;

    // Unity's main thread only.
    private RenderTexture? _screen;
    private RenderTexture? _frame;
    private bool _readbackPending;
    private float _nextCaptureTime;
    private int _screenWidth;
    private int _screenHeight;
    private AndroidJavaClass? _bitmapClass;
    private AndroidJavaClass? _byteBufferClass;
    private AndroidJavaObject? _argb8888;

    private readonly Stopwatch _statsWatch = new();
    private int _statsFrames;
    private int _statsDropped;
    private double _statsCaptureMs;
    private double _statsCopyMs;
    // Written by the worker.
    private double _statsHandOverMs;
    private int _statsHandedOver;

    public UnityReplayRecorder(IDiagnosticLogger? logger)
    {
        _logger = logger;
        _recorder = new RecorderProxy(this);
        _provider = new RecorderProvider(_recorder);
    }

    /// <summary>
    /// Replaces the replay integration the Android SDK added with one that records through this recorder.
    /// Runs in the options callback of the Android SDK's initialization, before the integrations register.
    /// </summary>
    public void Install(AndroidJavaObject androidOptions, AndroidJavaObject context)
    {
        using var appContext = context.Call<AndroidJavaObject>("getApplicationContext");
        using var dateProviderClass = new AndroidJavaClass("io.sentry.transport.CurrentDateProvider");
        using var dateProvider = dateProviderClass.CallStatic<AndroidJavaObject>("getInstance");
        using var replayClass = new AndroidJavaClass(ReplayIntegrationClass);

        // `ReplayIntegration(Context, ICurrentDateProvider, (() -> Recorder)?, ((SentryId) -> ReplayCache)?)`. Called
        // through JNI, as Unity can't pick a constructor for a null argument.
        var constructor = AndroidJNI.GetMethodID(replayClass.GetRawClass(), "<init>",
            "(Landroid/content/Context;" +
            "Lio/sentry/transport/ICurrentDateProvider;" +
            "Lkotlin/jvm/functions/Function0;" +
            "Lkotlin/jvm/functions/Function1;)V");
        ThrowOnJavaException("finding the ReplayIntegration constructor");

        var provider = IntPtr.Zero;
        var replayIntegration = IntPtr.Zero;
        try
        {
            provider = AndroidJNIHelper.CreateJavaProxy(_provider);
            var args = new jvalue[4];
            args[0].l = appContext.GetRawObject();
            args[1].l = dateProvider.GetRawObject();
            args[2].l = provider;
            args[3].l = IntPtr.Zero;
            replayIntegration = AndroidJNI.NewObject(replayClass.GetRawClass(), constructor, args);
            ThrowOnJavaException("creating the ReplayIntegration");

            RemoveReplayIntegrations(androidOptions);
            CallVoid(androidOptions, "addIntegration", "(Lio/sentry/Integration;)V", replayIntegration);
            CallVoid(androidOptions, "setReplayController", "(Lio/sentry/ReplayController;)V", replayIntegration);
        }
        finally
        {
            if (replayIntegration != IntPtr.Zero)
            {
                AndroidJNI.DeleteLocalRef(replayIntegration);
            }

            if (provider != IntPtr.Zero)
            {
                AndroidJNI.DeleteLocalRef(provider);
            }
        }

        _replayIntegration = androidOptions.Call<AndroidJavaObject>("getReplayController");
        _logger?.LogDebug("Session replay records the frames Unity captures.");
    }

    /// <summary>
    /// Starts capturing on Unity's main thread, once the Android SDK is initialized.
    /// </summary>
    public void Start()
    {
        if (_replayIntegration is null)
        {
            return;
        }

        using var looperClass = new AndroidJavaClass("android.os.Looper");
        using var mainLooper = looperClass.CallStatic<AndroidJavaObject>("getMainLooper");
        _mainHandler = new AndroidJavaObject("android.os.Handler", mainLooper);
        _statsWatch.Start();
        _worker = new Thread(HandOverFrames) { IsBackground = true, Name = "SentryReplayFrames" };
        _worker.Start();
        SentryMonoBehaviour.Instance.StartCoroutine(Run());
    }

    private static void RemoveReplayIntegrations(AndroidJavaObject androidOptions)
    {
        using var integrations = androidOptions.Call<AndroidJavaObject>("getIntegrations");
        for (var i = integrations.Call<int>("size") - 1; i >= 0; i--)
        {
            using var integration = integrations.Call<AndroidJavaObject>("get", i);
            using var integrationClass = integration.Call<AndroidJavaObject>("getClass");
            if (integrationClass.Call<string>("getName") == ReplayIntegrationClass)
            {
                integrations.Call<AndroidJavaObject>("remove", i)?.Dispose();
            }
        }
    }

    private static void CallVoid(AndroidJavaObject target, string method, string signature, IntPtr argument)
    {
        var methodId = AndroidJNI.GetMethodID(target.GetRawClass(), method, signature);
        ThrowOnJavaException($"finding '{method}'");
        var args = new jvalue[1];
        args[0].l = argument;
        AndroidJNI.CallVoidMethod(target.GetRawObject(), methodId, args);
        ThrowOnJavaException($"calling '{method}'");
    }

    private static void ThrowOnJavaException(string action)
    {
        var throwable = AndroidJNI.ExceptionOccurred();
        if (throwable == IntPtr.Zero)
        {
            return;
        }

        AndroidJNI.ExceptionDescribe();
        AndroidJNI.ExceptionClear();
        AndroidJNI.DeleteLocalRef(throwable);
        throw new InvalidOperationException($"A Java exception was thrown {action}. See logcat for details.");
    }

    // The recorder's calls, on Android's UI thread.

    // A started recorder captures until it's paused: the replay only resumes a paused one.
    private void OnStart()
    {
        _config = null;
        _resumed = true;
        _recording = true;
        Interlocked.Exchange(ref _configNeeded, 1);
    }

    private void OnConfigurationChanged(AndroidJavaObject config)
    {
        var frameConfig = new FrameConfig(
            config.Call<int>("getRecordingWidth"),
            config.Call<int>("getRecordingHeight"),
            config.Call<int>("getFrameRate"));
        _config = frameConfig;
        _logger?.LogDebug("Session replay records {0}x{1} at {2} fps.",
            frameConfig.Width, frameConfig.Height, frameConfig.FrameRate);
    }

    private void OnReset()
    {
        _config = null;
        Interlocked.Exchange(ref _configNeeded, 1);
    }

    private void OnStop()
    {
        _recording = false;
        _resumed = false;
        _config = null;
    }

    private void OnClose()
    {
        OnStop();
        _closed = true;
        _frameQueuedEvent.Set();
    }

    // Unity's main thread.

    private IEnumerator Run()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (!_closed)
        {
            yield return null;

            try
            {
                if (!_recording)
                {
                    ReleaseTextures();
                    continue;
                }

                RequestConfig();
                if (!IsCaptureDue())
                {
                    continue;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Session replay failed to prepare a frame.");
                continue;
            }

            yield return endOfFrame;

            try
            {
                Capture();
            }
            catch (Exception e)
            {
                _readbackPending = false;
                _logger?.LogError(e, "Session replay failed to capture a frame.");
            }
        }

        ReleaseTextures();
    }

    // The replay computes the frame config from the window size. It doesn't send one to a recorder of its own.
    // A change of Unity's screen size, e.g. on rotation, asks for a new one.
    private void RequestConfig()
    {
        var width = Screen.width;
        var height = Screen.height;
        var needed = Interlocked.Exchange(ref _configNeeded, 0) == 1;
        if (!needed && width == _screenWidth && height == _screenHeight)
        {
            return;
        }

        _screenWidth = width;
        _screenHeight = height;
        _mainHandler?.Call<bool>("post", new AndroidJavaRunnable(() => OnWindowSizeChanged(width, height)));
    }

    // On Android's UI thread, where the replay's own recorder reports the window size. Unity's screen size can be a
    // lower render resolution than the window. The replay scales recorded touches, which are in window pixels, by the
    // config it computes from the window size, so it gets the window's.
    private void OnWindowSizeChanged(int screenWidth, int screenHeight)
    {
        try
        {
            var replayIntegration = _replayIntegration;
            if (replayIntegration is null || _closed)
            {
                return;
            }

            // Starting or resetting the replay clears the config on this thread, so a config for this size is current.
            if (_config is not null && screenWidth == _configuredWidth && screenHeight == _configuredHeight)
            {
                return;
            }

            if (replayIntegration.Call<bool>("isRecording"))
            {
                var (width, height) = GetWindowSize() ?? (screenWidth, screenHeight);
                _configuredWidth = screenWidth;
                _configuredHeight = screenHeight;
                replayIntegration.Call("onWindowSizeChanged", width, height);
            }
            else if (_recording)
            {
                Interlocked.Exchange(ref _configNeeded, 1);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Session replay failed to update the frame size.");
        }
    }

    // The size of the activity's window, in pixels. Null before it's laid out.
    private static (int Width, int Height)? GetWindowSize()
    {
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        using var window = activity?.Call<AndroidJavaObject>("getWindow");
        using var decorView = window?.Call<AndroidJavaObject>("getDecorView");
        if (decorView is null)
        {
            return null;
        }

        var width = decorView.Call<int>("getWidth");
        var height = decorView.Call<int>("getHeight");
        return width > 0 && height > 0 ? (width, height) : null;
    }

    private bool IsCaptureDue()
    {
        if (!_resumed || _readbackPending || _config is not { } config)
        {
            return false;
        }

        var now = Time.realtimeSinceStartup;
        if (now < _nextCaptureTime)
        {
            return false;
        }

        var interval = 1f / config.FrameRate;
        _nextCaptureTime += interval;
        if (_nextCaptureTime < now)
        {
            _nextCaptureTime = now + interval;
        }

        return true;
    }

    private void Capture()
    {
        if (_closed || !_recording || !_resumed || _config is not { } config)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        EnsureTextures(config);

        var previous = RenderTexture.active;
        try
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_screen);
            SentryScreenshot.BlitScreen(_screen!, _frame!, topDown: true);
        }
        finally
        {
            RenderTexture.active = previous;
        }

        _readbackPending = true;
        AsyncGPUReadback.Request(_frame, 0, OnReadback);
        _statsCaptureMs += stopwatch.Elapsed.TotalMilliseconds;
    }

    private void EnsureTextures(FrameConfig config)
    {
        if (_screen is null || _screen.width != Screen.width || _screen.height != Screen.height)
        {
            ReleaseTexture(ref _screen);
            _screen = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
            _screen.Create();
        }

        if (_frame is null || _frame.width != config.Width || _frame.height != config.Height)
        {
            ReleaseTexture(ref _frame);
            // RGBA bytes, as Android's ARGB_8888 bitmaps hold them. In a linear color space, sRGB keeps the colors.
            var format = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? GraphicsFormat.R8G8B8A8_SRGB
                : GraphicsFormat.R8G8B8A8_UNorm;
            _frame = new RenderTexture(config.Width, config.Height, 0, format);
            _frame.Create();
        }
    }

    private void OnReadback(AsyncGPUReadbackRequest readback)
    {
        _readbackPending = false;
        try
        {
            if (readback.hasError)
            {
                Drop("the GPU readback failed");
                return;
            }

            // The config can change while the frame is read back, e.g. on rotation. A frame read back after the replay
            // paused would land after the segment that ended at the pause, so it's dropped.
            if (_closed || !_recording || !_resumed || _config is not { } config)
            {
                return;
            }

            var width = config.Width;
            var height = config.Height;
            var pixels = readback.GetData<sbyte>();
            if (readback.width != width || readback.height != height || pixels.Length != width * height * 4)
            {
                Drop("its size doesn't match the replay's");
                return;
            }

            if (_frameQueued)
            {
                Drop("the previous frame is still being handed over");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            if (_pixels.Length != pixels.Length)
            {
                _pixels = new sbyte[pixels.Length];
            }

            pixels.CopyTo(_pixels);
            _pixelsWidth = width;
            _pixelsHeight = height;
            _frameQueued = true;
            _frameQueuedEvent.Set();
            _statsCopyMs += stopwatch.Elapsed.TotalMilliseconds;
            _statsFrames++;
            LogStats();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Session replay failed to read back a frame.");
        }
    }

    // On the worker, attached to the JVM: handing a frame to Java takes several milliseconds, too long for Unity's
    // main thread.
    private void HandOverFrames()
    {
        AndroidJNI.AttachCurrentThread();
        try
        {
            while (true)
            {
                _frameQueuedEvent.WaitOne();
                if (_closed)
                {
                    return;
                }

                if (!_frameQueued)
                {
                    continue;
                }

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    PushFrame(_pixels, _pixelsWidth, _pixelsHeight);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Session replay failed to hand over a frame.");
                }
                finally
                {
                    _statsHandOverMs += stopwatch.Elapsed.TotalMilliseconds;
                    _statsHandedOver++;
                    _frameQueued = false;
                }
            }
        }
        finally
        {
            AndroidJNI.DetachCurrentThread();
        }
    }

    // A new bitmap for every frame: the replay compresses it on its own thread, after this returns.
    private void PushFrame(sbyte[] pixels, int width, int height)
    {
        _bitmapClass ??= new AndroidJavaClass("android.graphics.Bitmap");
        _byteBufferClass ??= new AndroidJavaClass("java.nio.ByteBuffer");
        if (_argb8888 is null)
        {
            using var configClass = new AndroidJavaClass("android.graphics.Bitmap$Config");
            _argb8888 = configClass.GetStatic<AndroidJavaObject>("ARGB_8888");
        }

        using var bitmap = _bitmapClass.CallStatic<AndroidJavaObject>("createBitmap", width, height, _argb8888);
        using var buffer = _byteBufferClass.CallStatic<AndroidJavaObject>("wrap", pixels);
        bitmap.Call("copyPixelsFromBuffer", buffer);
        // The screen's alpha channel isn't meant to be seen.
        bitmap.Call("setHasAlpha", false);
        // The replay may have paused while the frame was handed over.
        if (_recording && _resumed)
        {
            _replayIntegration?.Call("onScreenshotRecorded", bitmap);
        }
    }

    private void Drop(string reason)
    {
        _statsDropped++;
        _logger?.LogDebug("Session replay dropped a frame: {0}.", reason);
    }

    private void LogStats()
    {
        if (_statsWatch.Elapsed < StatsInterval)
        {
            return;
        }

        if (_statsFrames > 0)
        {
            var capture = _statsCaptureMs / _statsFrames;
            var copy = _statsCopyMs / _statsFrames;
            var handOver = _statsHandedOver == 0 ? 0 : _statsHandOverMs / _statsHandedOver;
            var seconds = _statsWatch.Elapsed.TotalSeconds;
            _logger?.LogDebug(
                FormattableString.Invariant(
                    $"Session replay captured {_statsFrames} frames in {seconds:F0}s ({_statsDropped} dropped). ") +
                FormattableString.Invariant(
                    $"Main thread per frame: {capture + copy:F2} ms (capture {capture:F2} ms, copy {copy:F2} ms). ") +
                FormattableString.Invariant(
                    $"Worker per frame: {handOver:F2} ms."));
        }

        _statsWatch.Restart();
        _statsFrames = 0;
        _statsDropped = 0;
        _statsCaptureMs = 0;
        _statsCopyMs = 0;
        _statsHandOverMs = 0;
        _statsHandedOver = 0;
    }

    private void ReleaseTextures()
    {
        // A pending readback still reads the frame texture.
        if (_readbackPending)
        {
            return;
        }

        ReleaseTexture(ref _screen);
        ReleaseTexture(ref _frame);
    }

    private static void ReleaseTexture(ref RenderTexture? texture)
    {
        if (texture is null)
        {
            return;
        }

        texture.Release();
        UnityEngine.Object.Destroy(texture);
        texture = null;
    }

    /// <summary>
    /// Implements <c>io.sentry.android.replay.Recorder</c>. Called on Android's UI thread.
    /// </summary>
    private sealed class RecorderProxy : AndroidJavaProxy
    {
        private readonly UnityReplayRecorder _owner;

        public RecorderProxy(UnityReplayRecorder owner) : base("io.sentry.android.replay.Recorder") => _owner = owner;

        // Overriding Invoke, as Unity doesn't find proxy methods defined by name.
        public override AndroidJavaObject? Invoke(string methodName, AndroidJavaObject[] args)
        {
            try
            {
                if (methodName is not "onConfigurationChanged" and not "equals" and not "hashCode" and not "toString")
                {
                    _owner._logger?.LogDebug("Session replay recorder: {0}.", methodName);
                }

                switch (methodName)
                {
                    case "start":
                        _owner.OnStart();
                        return null;
                    case "onConfigurationChanged" when args.Length == 1 && args[0] is not null:
                        using (args[0])
                        {
                            _owner.OnConfigurationChanged(args[0]);
                        }
                        return null;
                    case "resume":
                        _owner._resumed = true;
                        return null;
                    case "pause":
                        _owner._resumed = false;
                        return null;
                    case "reset":
                        _owner.OnReset();
                        return null;
                    case "stop":
                        _owner.OnStop();
                        return null;
                    case "close":
                        _owner.OnClose();
                        return null;
                    default:
                        // equals, hashCode and toString
                        return base.Invoke(methodName, args);
                }
            }
            catch (Exception e)
            {
                _owner._logger?.LogError(e, "Session replay recorder failed in '{0}'.", methodName);
                return null;
            }
        }
    }

    /// <summary>
    /// Implements <c>kotlin.jvm.functions.Function0</c>, returning the recorder. The replay calls it when it registers.
    /// </summary>
    private sealed class RecorderProvider : AndroidJavaProxy
    {
        private readonly RecorderProxy _recorder;

        public RecorderProvider(RecorderProxy recorder) : base("kotlin.jvm.functions.Function0") =>
            _recorder = recorder;

        public override AndroidJavaObject? Invoke(string methodName, AndroidJavaObject[] args)
        {
            if (methodName != "invoke")
            {
                return base.Invoke(methodName, args);
            }

            // Unity disposes the returned object, so each call returns a new one. Passing the proxy through Java gives
            // the Java object behind it.
            using var objects = new AndroidJavaClass("java.util.Objects");
            return objects.CallStatic<AndroidJavaObject>("requireNonNull", _recorder);
        }
    }
}
