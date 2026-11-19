using System;
using System.Collections;
using System.Diagnostics;
using Sentry.Extensibility;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Sentry.Unity.iOS;

/// <summary>
/// Session replay's frames when Unity captures them: sentry-cocoa asks for a frame on the main thread, and Unity
/// captures the screen at the end of its next frame and delivers it once the GPU has read it back.
/// </summary>
/// <remarks>
/// sentry-cocoa takes no further frame until it gets the one it asked for, so a request Unity took is always either
/// delivered or cancelled: cancelling completes it with the last frame. Everything here runs on Unity's main thread,
/// which on iOS is also UIKit's. After <see cref="MaxFailures"/> failures in a row the recorder gives up: sentry-cocoa
/// then waits on its next request, and Unity capture stays off for the rest of the process: later replays record no
/// frames either.
/// </remarks>
internal sealed class UnityReplayRecorder
{
    private static readonly TimeSpan StatsInterval = TimeSpan.FromSeconds(10);
    internal const int MaxFailures = 5;

    private readonly ICocoaReplay _replay;
    private readonly IDiagnosticLogger? _logger;

    private bool _running;
    private bool _paused;
    private int _failures;
    // The component the capture coroutine runs on, and whether that coroutine runs.
    private UnityReplayRecorderHost? _host;
    private bool _capturing;
    // The request being captured, or 0. Cancelling it clears it, so its frame doesn't complete a later request.
    private int _request;
    private bool _readbackPending;
    private RenderTexture? _screen;
    private RenderTexture? _frame;

    private readonly Stopwatch _statsWatch = new();
    private int _statsFrames;
    private int _statsCancelled;
    private double _statsCaptureMs;
    private double _statsDeliverMs;

    public UnityReplayRecorder(ICocoaReplay replay, IDiagnosticLogger? logger)
    {
        _replay = replay;
        _logger = logger;
    }

    public void Start(MonoBehaviour monoBehaviour)
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _statsWatch.Restart();
        _host = monoBehaviour.gameObject.AddComponent<UnityReplayRecorderHost>();
        _host.Recorder = this;
        OnHostEnabled();
    }

    // The coroutine stops when the host's GameObject is deactivated or destroyed, which leaves no one to complete
    // sentry-cocoa's request, so the host cancels it. Disabling the host stops the coroutine too.
    internal void OnHostEnabled()
    {
        if (!_running || _capturing || _host is null || !_host.isActiveAndEnabled)
        {
            return;
        }

        _capturing = true;
        _host.StartCoroutine(Run());
    }

    internal void OnHostDisabled()
    {
        if (!_capturing)
        {
            return;
        }

        _capturing = false;
        _host?.StopAllCoroutines();
        Cancel();
        ReleaseTextures();
    }

    /// <summary>
    /// Stops capturing while the application is in the background.
    /// </summary>
    public void Pause()
    {
        _paused = true;
        Cancel();
    }

    public void Resume() => _paused = false;

    public void Stop()
    {
        _running = false;
        _capturing = false;
        Cancel();
        ReleaseTextures();
        if (_host is not null)
        {
            _host.Recorder = null;
            _host.StopAllCoroutines();
            UnityEngine.Object.Destroy(_host);
            _host = null;
        }
    }

    // Gives up after MaxFailures failures in a row; until then the replay repeats the last frame.
    private void Fail(Exception? exception, string message)
    {
        _failures++;
        if (_failures < MaxFailures)
        {
            LogError(exception, "Session replay " + message + ". The replay repeats the last frame.");
            Cancel();
            return;
        }

        LogError(exception, "Session replay " + message + FormattableString.Invariant(
            $" {MaxFailures} times in a row. Unity stops capturing replay frames until the app restarts."));
        _running = false;
        Cancel();
        if (!_readbackPending)
        {
            ReleaseTextures();
        }
    }

    private void Cancel()
    {
        if (_request != 0)
        {
            _statsCancelled++;
        }

        _request = 0;
        // Also completes a request sentry-cocoa made that Unity hasn't taken yet.
        _replay.CancelFrameRequest();
    }

    private void LogError(Exception? exception, string message)
    {
        if (exception is null)
        {
            _logger?.LogError(message);
        }
        else
        {
            _logger?.LogError(exception, message);
        }
    }

    private IEnumerator Run()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (_running)
        {
            yield return null;

            if (_paused)
            {
                ReleaseTextures();
                continue;
            }

            if (_readbackPending)
            {
                continue;
            }

            int request;
            int width;
            int height;
            try
            {
                request = _replay.TakeFrameRequest(out width, out height);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Session replay failed to take a frame request. Unity stops capturing the " +
                                     "replay's frames.");
                _running = false;
                Cancel();
                break;
            }

            if (request == 0)
            {
                continue;
            }

            _request = request;
            // A window without a size has nothing to capture.
            if (width <= 0 || height <= 0)
            {
                Cancel();
                continue;
            }

            yield return endOfFrame;

            try
            {
                Capture(request, width, height);
            }
            catch (Exception e)
            {
                _readbackPending = false;
                if (_request == request)
                {
                    Fail(e, "failed to capture a frame");
                }
            }
        }

        _capturing = false;
        ReleaseTextures();
    }

    private void Capture(int request, int width, int height)
    {
        // Paused or stopped while waiting for the end of the frame.
        if (_request != request || !_running || _paused)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        EnsureTextures(width, height);

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
        AsyncGPUReadback.Request(_frame, 0, readback => OnReadback(readback, request));
        _statsCaptureMs += stopwatch.Elapsed.TotalMilliseconds;
    }

    // The frame is the size sentry-cocoa asks for: the window's size in points, which its touch tracker records in.
    private void EnsureTextures(int width, int height)
    {
        if (_screen is null || _screen.width != Screen.width || _screen.height != Screen.height)
        {
            ReleaseTexture(ref _screen);
            _screen = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
            _screen.Create();
        }

        if (_frame is null || _frame.width != width || _frame.height != height)
        {
            ReleaseTexture(ref _frame);
            // RGBA bytes in sRGB, as the native image takes them. In a linear color space, sRGB keeps the colors.
            var format = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? GraphicsFormat.R8G8B8A8_SRGB
                : GraphicsFormat.R8G8B8A8_UNorm;
            _frame = new RenderTexture(width, height, 0, format);
            _frame.Create();
        }
    }

    private void OnReadback(AsyncGPUReadbackRequest readback, int request)
    {
        _readbackPending = false;
        try
        {
            // Cancelled while the GPU read it back: the request is complete already.
            if (_request != request)
            {
                return;
            }

            if (readback.hasError)
            {
                Fail(null, "failed to read a frame back from the GPU");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var pixels = readback.GetData<byte>();
            if (pixels.Length != readback.width * readback.height * 4)
            {
                Fail(null, "read a frame of an unexpected size back from the GPU");
                return;
            }

            _request = 0;
            // The bridge copies the pixels before it returns: the readback's buffer is only valid in this callback.
            _replay.DeliverFrame(request, GetPixels(pixels), readback.width, readback.height);
            _failures = 0;

            _statsDeliverMs += stopwatch.Elapsed.TotalMilliseconds;
            _statsFrames++;
            LogStats();
        }
        catch (Exception e)
        {
            if (_request == request)
            {
                Fail(e, "failed to deliver a frame");
            }
        }
        finally
        {
            // Paused or stopped while the GPU read the frame back: the textures weren't released then.
            if (!_running || _paused || !_capturing)
            {
                ReleaseTextures();
            }
        }
    }

    private static unsafe IntPtr GetPixels(NativeArray<byte> pixels) =>
        (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(pixels);

    private void LogStats()
    {
        if (_statsWatch.Elapsed < StatsInterval)
        {
            return;
        }

        var capture = _statsCaptureMs / _statsFrames;
        var deliver = _statsDeliverMs / _statsFrames;
        _logger?.LogDebug(FormattableString.Invariant(
            $"Session replay captured {_statsFrames} frames in {_statsWatch.Elapsed.TotalSeconds:F0}s ({_statsCancelled} cancelled). ") +
            FormattableString.Invariant(
            $"Main thread per frame: {capture + deliver:F2} ms (capture {capture:F2} ms, deliver {deliver:F2} ms)."));

        _statsWatch.Restart();
        _statsFrames = 0;
        _statsCancelled = 0;
        _statsCaptureMs = 0;
        _statsDeliverMs = 0;
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
}

/// <summary>
/// Runs <see cref="UnityReplayRecorder"/>'s capture coroutine, and tells it when the coroutine stops with the component.
/// </summary>
internal sealed class UnityReplayRecorderHost : MonoBehaviour
{
    internal UnityReplayRecorder? Recorder { get; set; }

    private void OnEnable() => Recorder?.OnHostEnabled();

    private void OnDisable() => Recorder?.OnHostDisabled();
}
