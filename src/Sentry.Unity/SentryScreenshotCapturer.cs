using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sentry.Unity;

/// <summary>
/// Takes screenshots on demand, at the end of the frame an event was captured in, and hands them out as attachment
/// content that resolves once the SDK serializes the event's envelope.
/// </summary>
/// <remarks>
/// Requesting a screenshot is cheap and works on any thread. The main thread pays for taking it at the end of the frame
/// - a GPU copy and, where supported, an asynchronous readback into a pooled buffer. Encoding happens on the SDK's
/// background worker, straight from that buffer. Events captured close together share a screenshot.
/// </remarks>
internal class SentryScreenshotCapturer : IDisposable
{
    // Events within this window of a taken screenshot share it instead of requesting a new one. Bounds the cost of
    // an error on every frame.
    internal static readonly TimeSpan MinCaptureInterval = TimeSpan.FromMilliseconds(100);

    // Past this many screenshots waiting for their encode, events share the latest one instead.
    internal const int MaxPendingRequests = 2;

    // How old a shared screenshot may be before no screenshot is the better choice. A stale one misleads.
    internal static readonly TimeSpan MaxSharedAge = TimeSpan.FromSeconds(1);

    // How long the worker waits for the main thread to take a screenshot, e.g. while it's hanging or paused.
    internal static readonly TimeSpan CaptureTimeout = TimeSpan.FromSeconds(1);

    // Readback buffers kept for reuse: one per pending screenshot plus the one being taken. Buffers rented past this,
    // e.g. while dropped events wait to get collected, get disposed once returned.
    internal const int MaxBuffers = MaxPendingRequests + 1;

    private readonly SentryUnityOptions _options;
    private readonly bool _useAsyncReadback;

    // GPU side - main thread only
    private RenderTexture? _capture;
    private bool _flip;
    private int _width;
    private int _height;

    // Guarded by _lock
    private readonly object _lock = new();
    private SentryScreenshotRequest? _requested;
    private SentryScreenshotRequest? _latest;
    private readonly List<WeakReference<SentryScreenshotRequest>> _pending = new();
    private bool _stalled;

    // Readback buffers, guarded by _lock. In-use ones are tracked with the request holding them, so a buffer gets
    // reclaimed even if its event is dropped before the frame gets encoded.
    private readonly Stack<NativeArray<byte>> _freeBuffers = new();
    private readonly List<(WeakReference<SentryScreenshotRequest> Owner, NativeArray<byte> Buffer)> _usedBuffers = new();
    private int _bufferLength;

    private volatile bool _disposed;

    internal SentryScreenshotCapturer(SentryUnityOptions options)
        : this(options, SystemInfo.supportsAsyncGPUReadback) { }

    internal SentryScreenshotCapturer(SentryUnityOptions options, bool useAsyncReadback)
    {
        _options = options;
        _useAsyncReadback = useAsyncReadback;
    }

    internal bool IsDisposed => _disposed;

    // For testing
    internal int FreeBufferCount
    {
        get
        {
            lock (_lock)
            {
                ReclaimBuffers();
                return _freeBuffers.Count;
            }
        }
    }

    // For testing
    internal int UsedBufferCount
    {
        get
        {
            lock (_lock)
            {
                ReclaimBuffers();
                return _usedBuffers.Count;
            }
        }
    }

    // For testing
    internal bool HasRequest
    {
        get
        {
            lock (_lock)
            {
                return _requested is not null;
            }
        }
    }

    /// <summary>
    /// Requests a screenshot of the current frame. Returns null when none can be attached, e.g. because the main
    /// thread is not rendering.
    /// </summary>
    /// <remarks>
    /// Runs on any thread. A transform gets applied on the main thread once the screenshot is taken. A callback to the
    /// pixels gets applied on the SDK's worker right before encoding.
    /// </remarks>
    internal virtual IAttachmentContent? TryRequest(Func<Texture2D, Texture2D?>? transform = null,
        Func<SentryScreenshotPixels, bool>? beforeEncode = null)
    {
        if (_disposed)
        {
            return null;
        }

        lock (_lock)
        {
            if (_stalled)
            {
                _options.LogDebug("Screenshot skipped. The main thread did not take the last one in time.");
                return null;
            }

            if (_requested is { } requested)
            {
                return Attach(requested, transform, beforeEncode);
            }

            if (_latest is { } recent && IsShareable(recent, MinCaptureInterval) && CanServe(recent, transform))
            {
                return Attach(recent, transform, beforeEncode);
            }

            if (CountPending() >= MaxPendingRequests)
            {
                if (_latest is { } latest && IsShareable(latest, MaxSharedAge) && CanServe(latest, transform))
                {
                    return Attach(latest, transform, beforeEncode);
                }

                _options.LogDebug("Screenshot skipped. Too many screenshots are waiting to be sent.");
                return null;
            }

            _requested = new SentryScreenshotRequest();
            return Attach(_requested, transform, beforeEncode);
        }
    }

    // Must be called while holding _lock
    private IAttachmentContent Attach(SentryScreenshotRequest request, Func<Texture2D, Texture2D?>? transform,
        Func<SentryScreenshotPixels, bool>? beforeEncode)
    {
        Task<byte[]?>? transformed = null;
        if (transform is null)
        {
            request.HasFrameConsumers = true;
            if (beforeEncode is not null)
            {
                request.TrySetBeforeEncode(beforeEncode);
            }
        }
        else
        {
            // Not yet taken or already transformed, see CanServe
            transformed = request.TryAddTransform(transform);
        }

        return new SentryScreenshotAttachmentContent(this, request, transformed, _options);
    }

    // Whether a screenshot that was already taken can serve another event. A transform only runs once per
    // screenshot, so with one set, only a transformed screenshot can be shared. Without, the frame must still be
    // encodable - its pixels get released right away when only the transform uses them.
    private static bool CanServe(SentryScreenshotRequest request, Func<Texture2D, Texture2D?>? transform)
    {
        if (transform is not null)
        {
            return request.HasTransform;
        }

        return !request.IsCompleted || request.Frame.Result is not { } frame || frame.IsEncodable;
    }

    // Must be called while holding _lock
    private int CountPending()
    {
        // Captures drop out once nothing is left to encode, or once the attachments holding on to them are gone -
        // e.g. because the event got dropped before being sent
        _pending.RemoveAll(reference => !reference.TryGetTarget(out var request) || request.IsSettled);
        ReclaimBuffers();
        return _pending.Count;
    }

    // Must be called while holding _lock
    private NativeArray<byte> RentBuffer(SentryScreenshotRequest owner, int length)
    {
        ReclaimBuffers();

        if (_bufferLength != length)
        {
            // The resolution changed. Buffers in use get disposed once returned.
            while (_freeBuffers.Count > 0)
            {
                _freeBuffers.Pop().Dispose();
            }

            _bufferLength = length;
        }

        var buffer = _freeBuffers.Count > 0
            ? _freeBuffers.Pop()
            : new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        _usedBuffers.Add((new WeakReference<SentryScreenshotRequest>(owner), buffer));
        return buffer;
    }

    // Called by the frame once its pixels are no longer needed, on whichever thread encoded or released them
    private void ReturnBuffer(NativeArray<byte> buffer)
    {
        lock (_lock)
        {
            var index = _usedBuffers.FindIndex(used => used.Buffer.Equals(buffer));
            if (index < 0)
            {
                // Already reclaimed
                return;
            }

            _usedBuffers.RemoveAt(index);
            Recycle(buffer);
        }
    }

    // Must be called while holding _lock. A request that got collected took its frame with it, so nothing can return
    // its buffer anymore.
    private void ReclaimBuffers()
    {
        for (var i = _usedBuffers.Count - 1; i >= 0; i--)
        {
            if (!_usedBuffers[i].Owner.TryGetTarget(out _))
            {
                var buffer = _usedBuffers[i].Buffer;
                _usedBuffers.RemoveAt(i);
                Recycle(buffer);
            }
        }
    }

    // Must be called while holding _lock
    private void Recycle(NativeArray<byte> buffer)
    {
        if (_disposed || buffer.Length != _bufferLength || _freeBuffers.Count >= MaxBuffers)
        {
            buffer.Dispose();
        }
        else
        {
            _freeBuffers.Push(buffer);
        }
    }

    private static bool IsShareable(SentryScreenshotRequest request, TimeSpan maxAge)
    {
        if (request.IsCompleted && request.Frame.Result is null)
        {
            return false;
        }

        var age = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - request.TakenTimestamp) / (double)Stopwatch.Frequency);
        return age < maxAge;
    }

    /// <summary>
    /// Waits for the screenshot to be taken. Gives up after <see cref="CaptureTimeout"/>, which also skips screenshots
    /// until the main thread reaches the end of a frame again.
    /// </summary>
    internal async Task WaitAsync(SentryScreenshotRequest request)
    {
        var completed = await Task.WhenAny(request.Frame, Task.Delay(CaptureTimeout)).ConfigureAwait(false);
        if (completed == request.Frame)
        {
            return;
        }

        _options.LogDebug("Screenshot was not taken in time. Skipping screenshots until the main thread catches up.");

        lock (_lock)
        {
            _stalled = true;
            if (ReferenceEquals(_requested, request))
            {
                _requested = null;
            }
        }

        request.Abandon();
    }

    /// <summary>
    /// Takes the requested screenshot, if any. Must run on the main thread at end of frame - capturing mid-frame
    /// yields an incomplete image.
    /// </summary>
    internal virtual void OnEndOfFrame()
    {
        if (_disposed)
        {
            return;
        }

        SentryScreenshotRequest? request;
        lock (_lock)
        {
            _stalled = false;
            request = _requested;
            _requested = null;

            if (request is null)
            {
                return;
            }

            request.TakenTimestamp = Stopwatch.GetTimestamp();
            _latest = request;
            _pending.Add(new WeakReference<SentryScreenshotRequest>(request));
        }

        Take(request);
    }

    private void Take(SentryScreenshotRequest request)
    {
        var (screenWidth, screenHeight) = GetScreenSize();
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            request.Abandon();
            return;
        }

        var (targetWidth, targetHeight) = SentryScreenshot.GetTargetSize(_options.ScreenshotQuality, screenWidth, screenHeight);
        EnsureTextures(targetWidth, targetHeight);

        var previous = RenderTexture.active;

        // The capture copies the screen into the target as is instead of scaling it, so it needs one at the
        // screen's resolution. It's only needed for the duration of the frame, and the pool hands it out again on
        // the next screenshot.
        var screen = RenderTexture.GetTemporary(screenWidth, screenHeight, 0, RenderTextureFormat.ARGB32);
        try
        {
            CaptureScreen(screen);

            // Scales the capture down to the target size. The captured image is mirrored on some platforms, which
            // gets fixed in the same pass so every consumer gets it in the right orientation.
            // See https://docs.unity3d.com/2019.4/Documentation/Manual/SL-PlatformDifferences.html for more info.
            // Note, we can't use the `UNITY_UV_STARTS_AT_TOP` macro because it's only available in shaders.
            if (_flip)
            {
                Graphics.Blit(screen, _capture, new Vector2(1, -1), new Vector2(0, 1));
            }
            else
            {
                Graphics.Blit(screen, _capture);
            }

            if (_useAsyncReadback)
            {
                // The readback copies the texture as of now, so later captures into it don't affect this one
                RequestReadback(request, _capture!);
            }
            else
            {
                // Stalls the main thread until the GPU caught up, but only on frames that had an error
                request.Complete(ReadPixels(request, _capture!), _options);
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to take the screenshot.");
            request.Abandon();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(screen);
        }
    }

    // For testing
    internal virtual (int Width, int Height) GetScreenSize() => (Screen.width, Screen.height);

    // For testing
    internal virtual void CaptureScreen(RenderTexture target) => ScreenCapture.CaptureScreenshotIntoRenderTexture(target);

    private void RequestReadback(SentryScreenshotRequest request, RenderTexture source)
    {
        NativeArray<byte> buffer;
        lock (_lock)
        {
            buffer = RentBuffer(request, _width * _height * 4);
        }

        try
        {
            // Reads back straight into the buffer, which then gets encoded as is - nothing gets copied on the main thread
            AsyncGPUReadback.RequestIntoNativeArray(ref buffer, source, 0, TextureFormat.RGBA32,
                readback => OnReadbackComplete(request, readback, buffer));
        }
        catch
        {
            ReturnBuffer(buffer);
            throw;
        }
    }

    private void OnReadbackComplete(SentryScreenshotRequest request, AsyncGPUReadbackRequest readback, NativeArray<byte> buffer)
    {
        if (_disposed || readback.hasError)
        {
            ReturnBuffer(buffer);
            request.Abandon();
            return;
        }

        try
        {
            request.Complete(new SentryScreenshotFrame(buffer, readback.width, readback.height, ReturnBuffer), _options);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to read back the screenshot.");
            ReturnBuffer(buffer);
            request.Abandon();
        }
    }

    private SentryScreenshotFrame ReadPixels(SentryScreenshotRequest request, RenderTexture source)
    {
        NativeArray<byte> buffer;
        lock (_lock)
        {
            buffer = RentBuffer(request, _width * _height * 4);
        }

        var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = source;
            texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            buffer.CopyFrom(texture.GetRawTextureData<byte>());
            return new SentryScreenshotFrame(buffer, _width, _height, ReturnBuffer);
        }
        catch
        {
            ReturnBuffer(buffer);
            throw;
        }
        finally
        {
            UnityEngine.Object.Destroy(texture);
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
        _flip = SentrySystemInfoAdapter.Instance.GraphicsUVStartsAtTop ?? true;
        _width = width;
        _height = height;

        _options.LogDebug("Screenshot textures allocated at {0}x{1}.", width, height);
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

        // Releases everything waiting, so that flushing on close doesn't wait for screenshots that won't be taken
        var waiting = new List<SentryScreenshotRequest>();
        lock (_lock)
        {
            if (_requested is not null)
            {
                waiting.Add(_requested);
            }

            foreach (var reference in _pending)
            {
                if (reference.TryGetTarget(out var request))
                {
                    waiting.Add(request);
                }
            }

            _requested = null;
            _latest = null;
            _pending.Clear();

            // Buffers in use get disposed once returned, or here if their request is gone already
            ReclaimBuffers();
            while (_freeBuffers.Count > 0)
            {
                _freeBuffers.Pop().Dispose();
            }
        }

        foreach (var request in waiting)
        {
            request.Abandon();
        }

        // Off the main thread, the coroutine releases the GPU resources once it notices the cache got disposed.
        if (MainThreadData.IsMainThread())
        {
            ReleaseResources();
        }
    }
}
