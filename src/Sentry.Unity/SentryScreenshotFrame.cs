using System;
using System.Diagnostics;
using Sentry.Extensibility;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Sentry.Unity;

/// <summary>
/// A frame read back from the GPU. Its pixels never change once the cache published it, so it can get encoded on
/// any thread.
/// </summary>
/// <remarks>
/// The frame gets encoded at most once. The first consumer pays for it and every other event attaching the same frame
/// reuses the result. The pixels are dropped as soon as they are no longer needed: once the frame got encoded and the
/// cache moved on to a newer one.
/// </remarks>
internal sealed class SentryScreenshotFrame
{
    private static readonly ProfilerMarker EncodeMarker = new("Sentry.Screenshot.Encode");

    // Serializes encoders. Retiring never takes it, so the main thread can't get blocked by an encode in progress.
    private readonly object _encodeLock = new();
    private volatile byte[]? _pixels;
    private byte[]? _encoded;
    private volatile bool _encodeStarted;
    private volatile bool _retired;

    internal SentryScreenshotFrame(byte[] pixels, int width, int height)
    {
        _pixels = pixels;
        Width = width;
        Height = height;
    }

    internal int Width { get; }
    internal int Height { get; }

    /// <summary>
    /// Set once something holds on to the frame beyond the cache, e.g. an attachment. The cache never reuses the
    /// pixel buffer of a pinned frame. Guarded by the cache's lock.
    /// </summary>
    internal bool Pinned { get; set; }

    /// <summary>
    /// Whether the encode has started. Readable without blocking on an encode in progress.
    /// </summary>
    internal bool IsEncodeStarted => _encodeStarted;

    /// <summary>
    /// Pixels in RGBA32, bottom row first. Null once dropped.
    /// </summary>
    internal byte[]? Pixels => _pixels;

    /// <summary>
    /// Returns the frame encoded as JPG, encoding it on the calling thread if no one did so yet. Returns null if the
    /// encode failed.
    /// </summary>
    internal byte[]? GetEncoded(SentryUnityOptions options)
    {
        lock (_encodeLock)
        {
            if (_encodeStarted)
            {
                return _encoded;
            }

            // Taking the reference before flagging the encode as started means Retire, which drops the pixels of
            // started encodes, can't pull them away from under this one
            var pixels = _pixels;
            _encodeStarted = true;

            if (pixels is null)
            {
                return null;
            }

            try
            {
                using var _ = EncodeMarker.Auto();
                var start = Stopwatch.GetTimestamp();

                // Thread safe, so it can run wherever the attachment gets serialized
                var bytes = ImageConversion.EncodeArrayToJPG(pixels, GraphicsFormat.R8G8B8A8_UNorm,
                    (uint)Width, (uint)Height, 0, options.ScreenshotCompression);

                if (bytes is { Length: > 0 })
                {
                    _encoded = bytes;

                    // The profiler under-reports long samples on background threads, so the duration gets logged
                    var elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                    options.LogDebug("Screenshot encoded in {0} ms to {1} bytes.", elapsedMs, bytes.Length);
                }
                else
                {
                    options.LogWarning("Screenshot encoding returned empty data.");
                }
            }
            catch (Exception e)
            {
                options.LogError(e, "Failed to encode the screenshot.");
            }
            finally
            {
                if (_retired)
                {
                    _pixels = null;
                }
            }

            return _encoded;
        }
    }

    /// <summary>
    /// Called by the cache, under its lock, once a newer frame replaced this one. Hands back the pixel buffer for
    /// reuse if nothing else holds on to the frame.
    /// </summary>
    internal byte[]? Retire()
    {
        _retired = true;

        var pixels = _pixels;
        if (!Pinned)
        {
            // Nothing can encode an unpinned frame, and pinning happens under the cache's lock as well
            _pixels = null;
            return pixels;
        }

        // A pinned frame keeps its pixels until its encode started. An encode in progress holds its own reference.
        if (_encodeStarted)
        {
            _pixels = null;
        }

        return null;
    }
}
