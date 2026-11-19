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
/// which on iOS is also UIKit's.
/// </remarks>
internal sealed class UnityReplayRecorder
{
    private static readonly TimeSpan StatsInterval = TimeSpan.FromSeconds(10);

    private readonly ICocoaReplay _replay;
    private readonly IDiagnosticLogger? _logger;

    private bool _running;
    private bool _paused;
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
        monoBehaviour.StartCoroutine(Run());
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
        Cancel();
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
                _logger?.LogError(e, "Session replay failed to take a frame request.");
                yield break;
            }

            if (request == 0)
            {
                continue;
            }

            _request = request;
            yield return endOfFrame;

            try
            {
                Capture(request, width, height);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Session replay failed to capture a frame.");
                _readbackPending = false;
                if (_request == request)
                {
                    Cancel();
                }
            }
        }

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
                _logger?.LogDebug("Session replay's GPU readback failed. The replay repeats the last frame.");
                Cancel();
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var pixels = readback.GetData<byte>();
            if (pixels.Length != readback.width * readback.height * 4)
            {
                _logger?.LogDebug("Session replay's GPU readback has an unexpected size. The replay repeats the last frame.");
                Cancel();
                return;
            }

            _request = 0;
            // The bridge copies the pixels before it returns: the readback's buffer is only valid in this callback.
            _replay.DeliverFrame(request, GetPixels(pixels), readback.width, readback.height);

            _statsDeliverMs += stopwatch.Elapsed.TotalMilliseconds;
            _statsFrames++;
            LogStats();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Session replay failed to deliver a frame.");
            if (_request == request)
            {
                Cancel();
            }
        }
        finally
        {
            // Paused or stopped while the GPU read the frame back: the textures weren't released then.
            if (!_running || _paused)
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
