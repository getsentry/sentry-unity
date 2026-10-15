using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sentry.Unity;

/// <summary>
/// Holds the most recently completed frame so that it can be attached to an event synchronously,
/// from within the event processor pipeline, instead of being sent as a separate envelope.
/// </summary>
/// <remarks>
/// The frame gets captured on the GPU and, where supported, copied to the CPU via an asynchronous readback.
/// Attaching it to an event then hands out the frame, which only gets encoded once the SDK serializes the envelope
/// on its background worker. Without async readback support the frame stays on the GPU and has to be read back and
/// encoded synchronously, which limits attaching it to events captured on the main thread.
/// </remarks>
internal class SentryScreenshotCache : IDisposable
{
    // A readback completes within a few frames. Past this, it's considered lost so a missing callback
    // can't block all further refreshes.
    private const int ReadbackTimeoutFrames = 30;

    // Frames held on to by attachments waiting for their envelope to get serialized. Past this, attaching encodes
    // right away, which bounds the memory pending screenshots can hold on to.
    internal const int MaxPendingFrames = 2;

    private readonly SentryUnityOptions _options;
    private readonly bool _useAsyncReadback;
    private readonly Action<AsyncGPUReadbackRequest> _onReadbackComplete;

    // GPU side - main thread only
    private RenderTexture? _capture;
    private RenderTexture? _oriented;
    private int _width;
    private int _height;
    private bool _readbackInFlight;
    private int _readbackRequestFrame;

    // CPU side - guarded by _lock
    private readonly object _lock = new();
    private SentryScreenshotFrame? _frame;
    private byte[]? _spare;
    private readonly List<WeakReference<SentryScreenshotFrame>> _pendingFrames = new();

    // Without async readback the frame only exists on the GPU - guarded by _lock
    private bool _gpuHasContent;
    private int _gpuGeneration;
    private byte[]? _gpuEncoded;
    private int _gpuEncodedGeneration = -1;

    private volatile bool _disposed;

    internal SentryScreenshotCache(SentryUnityOptions options)
        : this(options, SystemInfo.supportsAsyncGPUReadback) { }

    internal SentryScreenshotCache(SentryUnityOptions options, bool useAsyncReadback)
    {
        _options = options;
        _useAsyncReadback = useAsyncReadback;
        _onReadbackComplete = OnReadbackComplete;
    }

    internal bool HasContent
    {
        get
        {
            lock (_lock)
            {
                return _frame is not null || _gpuHasContent;
            }
        }
    }

    internal bool IsDisposed => _disposed;

    // For testing
    internal SentryScreenshotFrame? CurrentFrame
    {
        get
        {
            lock (_lock)
            {
                return _frame;
            }
        }
    }

    /// <summary>
    /// Captures the current back buffer into the cache. Must run on the main thread at end of frame -
    /// capturing mid-frame yields an incomplete image.
    /// </summary>
    internal virtual void Refresh()
    {
        if (_disposed)
        {
            return;
        }

        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        var (targetWidth, targetHeight) = SentryScreenshot.GetTargetSize(_options.ScreenshotQuality, screenWidth, screenHeight);
        EnsureTextures(targetWidth, targetHeight);

        var previous = RenderTexture.active;
        try
        {
            // Capturing straight into the downscaled texture keeps this - the only recurring cost of
            // attaching screenshots - cheap.
            CaptureScreen(_capture!);

            var source = _capture!;
            if (_oriented != null)
            {
                Graphics.Blit(_capture, _oriented, new Vector2(1, -1), new Vector2(0, 1));
                source = _oriented;
            }

            if (_useAsyncReadback)
            {
                RequestReadback(source);
            }
            else
            {
                lock (_lock)
                {
                    _gpuGeneration++;
                    _gpuHasContent = true;
                }
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to refresh the screenshot cache.");
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    // For testing
    internal virtual void CaptureScreen(RenderTexture target) => ScreenCapture.CaptureScreenshotIntoRenderTexture(target);

    private void RequestReadback(RenderTexture source)
    {
        if (_readbackInFlight)
        {
            // Skipping this refresh means the cache trails by one interval at most, instead of piling
            // up readbacks on a slow GPU.
            if (Time.frameCount - _readbackRequestFrame < ReadbackTimeoutFrames)
            {
                return;
            }

            _options.LogDebug("Screenshot readback did not complete in time. Requesting a new one.");
        }

        _readbackInFlight = true;
        _readbackRequestFrame = Time.frameCount;
        AsyncGPUReadback.Request(source, 0, TextureFormat.RGBA32, _onReadbackComplete);
    }

    private void OnReadbackComplete(AsyncGPUReadbackRequest request)
    {
        _readbackInFlight = false;

        if (_disposed || request.hasError)
        {
            return;
        }

        try
        {
            var data = request.GetData<byte>();

            // The new frame goes into a buffer no one else can see, so attachments holding on to the current
            // frame are never affected
            byte[]? buffer;
            lock (_lock)
            {
                buffer = _spare is not null && _spare.Length == data.Length ? _spare : null;
                _spare = null;
            }

            buffer ??= new byte[data.Length];
            data.CopyTo(buffer);

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                var previous = _frame;
                _frame = new SentryScreenshotFrame(buffer, request.width, request.height);

                // Only a frame nothing holds on to hands back its buffer. A buffer gets allocated whenever an
                // event attached the previous frame.
                _spare = previous?.Retire();
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to read back the screenshot.");
        }
    }

    /// <summary>
    /// Returns the cached frame as attachment content. Returns null when the cache has not been populated yet or the
    /// frame can't be attached on the calling thread.
    /// </summary>
    /// <remarks>
    /// With async readback support this runs on any thread and the content defers encoding until it gets read.
    /// </remarks>
    internal virtual IAttachmentContent? TryGetLatestContent()
    {
        if (_disposed)
        {
            return null;
        }

        if (!_useAsyncReadback)
        {
            return TryEncodeLatest() is { } bytes ? new ByteAttachmentContent(bytes) : null;
        }

        SentryScreenshotFrame? frame;
        bool deferEncode;
        lock (_lock)
        {
            frame = _frame;
            if (frame is null)
            {
                _options.LogDebug("Screenshot cache is empty. Skipping.");
                return null;
            }

            // A frame that is already pinned costs nothing extra to hand out again
            deferEncode = frame.Pinned || CountPendingFrames() < MaxPendingFrames;
            if (!frame.Pinned)
            {
                frame.Pinned = true;
                if (deferEncode)
                {
                    _pendingFrames.Add(new WeakReference<SentryScreenshotFrame>(frame));
                }
            }
        }

        if (deferEncode)
        {
            return new SentryScreenshotAttachmentContent(frame, _options);
        }

        _options.LogDebug("Too many screenshots waiting to be sent. Encoding right away.");
        return frame.GetEncoded(_options) is { } encoded ? new ByteAttachmentContent(encoded) : null;
    }

    // Must be called while holding _lock
    private int CountPendingFrames()
    {
        // Frames drop out once their encode started, or once the attachments holding on to them are gone - e.g.
        // because the event got dropped before being sent
        _pendingFrames.RemoveAll(reference => !reference.TryGetTarget(out var frame) || frame.IsEncodeStarted);
        return _pendingFrames.Count;
    }

    /// <summary>
    /// Encodes the cached frame as JPG right away. Returns null when the cache has not been populated yet, the
    /// transform discarded the screenshot, or the encode is not possible on the calling thread.
    /// </summary>
    /// <remarks>
    /// Without a transform, this can run on any thread when async GPU readback is supported. Applying a
    /// transform requires the main thread since it operates on a <see cref="Texture2D"/>.
    /// </remarks>
    internal virtual byte[]? TryEncodeLatest(Func<Texture2D, Texture2D?>? transform = null)
    {
        if (_disposed)
        {
            return null;
        }

        try
        {
            if (transform is not null)
            {
                return EncodeLatest(transform);
            }

            return _useAsyncReadback ? EncodeLatestFrame() : EncodeLatestGpu();
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to encode the cached screenshot.");
            return null;
        }
    }

    private byte[]? EncodeLatestFrame()
    {
        SentryScreenshotFrame? frame;
        lock (_lock)
        {
            frame = _frame;
            if (frame is null)
            {
                _options.LogDebug("Screenshot cache is empty. Skipping.");
                return null;
            }

            frame.Pinned = true;
        }

        return frame.GetEncoded(_options);
    }

    private byte[]? EncodeLatestGpu()
    {
        int generation;
        lock (_lock)
        {
            if (!_gpuHasContent)
            {
                _options.LogDebug("Screenshot cache is empty. Skipping.");
                return null;
            }

            // Every event between two refreshes sees the same frame, so the encode is only worth doing once.
            if (_gpuEncodedGeneration == _gpuGeneration)
            {
                return _gpuEncoded;
            }

            generation = _gpuGeneration;
        }

        // Without a CPU copy the frame has to be read back from the GPU, which is a main thread only operation.
        if (!MainThreadData.IsMainThread())
        {
            _options.LogDebug("Screenshot skipped. Without async GPU readback support, screenshots can only be attached on the main thread.");
            return null;
        }

        Texture2D? screenshot = null;
        try
        {
            screenshot = ReadTexture();

            var bytes = screenshot.EncodeToJPG(_options.ScreenshotCompression);

            if (bytes is null || bytes.Length == 0)
            {
                _options.LogWarning("Screenshot encoding returned empty data.");
                return null;
            }

            lock (_lock)
            {
                _gpuEncoded = bytes;
                _gpuEncodedGeneration = generation;
            }

            return bytes;
        }
        finally
        {
            if (screenshot != null)
            {
                UnityEngine.Object.Destroy(screenshot);
            }
        }
    }

    private byte[]? EncodeLatest(Func<Texture2D, Texture2D?> transform)
    {
        if (!MainThreadData.IsMainThread())
        {
            _options.LogDebug("Screenshot skipped. The BeforeSendScreenshot callback can only be invoked on the main thread.");
            return null;
        }

        Texture2D? screenshot = null;
        try
        {
            screenshot = CreateTexture();
            if (screenshot == null)
            {
                _options.LogDebug("Screenshot cache is empty. Skipping.");
                return null;
            }

            // A transform may return a different image per event, so its result does not get cached.
            var transformed = transform.Invoke(screenshot);
            if (transformed == null)
            {
                return null;
            }

            if (transformed != screenshot)
            {
                UnityEngine.Object.Destroy(screenshot);
                screenshot = transformed;
            }

            var bytes = screenshot.EncodeToJPG(_options.ScreenshotCompression);

            if (bytes is null || bytes.Length == 0)
            {
                _options.LogWarning("Screenshot encoding returned empty data.");
                return null;
            }

            return bytes;
        }
        finally
        {
            if (screenshot != null)
            {
                UnityEngine.Object.Destroy(screenshot);
            }
        }
    }

    // Main thread only
    private Texture2D? CreateTexture()
    {
        SentryScreenshotFrame? frame;
        lock (_lock)
        {
            if (!_useAsyncReadback)
            {
                return _gpuHasContent ? ReadTexture() : null;
            }

            frame = _frame;
        }

        // The current frame's pixels only get replaced on the main thread, by the readback callback
        if (frame?.Pixels is not { } pixels)
        {
            return null;
        }

        var texture = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(pixels);
        texture.Apply();
        return texture;
    }

    private Texture2D ReadTexture()
    {
        var previous = RenderTexture.active;
        try
        {
            RenderTexture.active = _oriented != null ? _oriented : _capture;
            var screenshot = new Texture2D(_width, _height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            screenshot.Apply();
            return screenshot;
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    private void EnsureTextures(int width, int height)
    {
        if (_capture != null && _width == width && _height == height)
        {
            return;
        }

        ReleaseResources();

        _capture = CreateRenderTexture(width, height, "Sentry.ScreenshotCapture");

        // The captured image is mirrored on some platforms. Mirroring it back right away on the GPU means
        // every consumer of the cache gets it in the right orientation.
        // See https://docs.unity3d.com/2019.4/Documentation/Manual/SL-PlatformDifferences.html for more info.
        // Note, we can't use the `UNITY_UV_STARTS_AT_TOP` macro because it's only available in shaders.
        if (SentrySystemInfoAdapter.Instance.GraphicsUVStartsAtTop ?? true)
        {
            _oriented = CreateRenderTexture(width, height, "Sentry.ScreenshotOriented");
        }

        _width = width;
        _height = height;

        // A frame read back to the CPU stays valid, if sized differently. A frame on the GPU is gone.
        lock (_lock)
        {
            _gpuHasContent = false;
        }

        _options.LogDebug("Screenshot cache allocated at {0}x{1}.", width, height);
    }

    private static RenderTexture CreateRenderTexture(int width, int height, string name)
    {
        var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { name = name };
        texture.Create();
        return texture;
    }

    /// <summary>
    /// Releases the GPU resources. Must run on the main thread.
    /// </summary>
    internal void ReleaseResources()
    {
        ReleaseRenderTexture(ref _capture);
        ReleaseRenderTexture(ref _oriented);
    }

    private static void ReleaseRenderTexture(ref RenderTexture? texture)
    {
        if (texture == null)
        {
            return;
        }

        texture.Release();
        UnityEngine.Object.Destroy(texture);
        texture = null;
    }

    public void Dispose()
    {
        _disposed = true;

        // Attachments still holding on to a frame can encode it regardless
        lock (_lock)
        {
            _frame = null;
            _spare = null;
            _pendingFrames.Clear();
            _gpuHasContent = false;
            _gpuEncoded = null;
            _gpuEncodedGeneration = -1;
        }

        // Off the main thread, the refresh coroutine releases the GPU resources once it notices the
        // cache got disposed.
        if (MainThreadData.IsMainThread())
        {
            ReleaseResources();
        }
    }
}
