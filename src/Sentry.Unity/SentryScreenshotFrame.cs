using System;
using Sentry.Extensibility;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Sentry.Unity;

/// <summary>
/// A frame read back from the GPU into a buffer rented from the capturer. Its pixels only change through the
/// <c>BeforeSendScreenshotPixels</c> callback right before encoding, so it can get encoded on any thread.
/// </summary>
/// <remarks>
/// The frame gets encoded at most once. The first consumer pays for it and every other event attaching the same frame
/// reuses the result. The buffer goes back to the capturer once encoded or released.
/// </remarks>
internal sealed class SentryScreenshotFrame
{
    // Serializes encoding and releasing. The buffer gets handed back only after leaving it, so the capturer's lock is
    // never taken while holding this one.
    private readonly object _lock = new();
    private readonly Action<NativeArray<byte>> _returnBuffer;
    private NativeArray<byte> _buffer;
    private byte[]? _encoded;
    private volatile bool _encodeStarted;
    private volatile bool _released;

    /// <summary>
    /// Wraps a rented buffer holding RGBA32 pixels, bottom row first, which gets handed to
    /// <paramref name="returnBuffer"/> once released.
    /// </summary>
    internal SentryScreenshotFrame(NativeArray<byte> buffer, int width, int height, Action<NativeArray<byte>> returnBuffer)
    {
        _buffer = buffer;
        _returnBuffer = returnBuffer;
        Width = width;
        Height = height;
    }

    internal int Width { get; }
    internal int Height { get; }

    /// <summary>
    /// Whether the encode has started. Readable without blocking on an encode in progress.
    /// </summary>
    internal bool IsEncodeStarted => _encodeStarted;

    /// <summary>
    /// Whether the frame can still be encoded, i.e. it either was already or still holds its pixels. Readable without
    /// blocking on an encode in progress.
    /// </summary>
    internal bool IsEncodable => _encodeStarted || !_released;

    /// <summary>
    /// Loads the pixels into the texture. Returns false once they're released.
    /// </summary>
    internal bool TryLoadInto(Texture2D texture)
    {
        lock (_lock)
        {
            if (_released)
            {
                return false;
            }

            texture.LoadRawTextureData(_buffer);
            return true;
        }
    }

    /// <summary>
    /// Returns the frame encoded as JPG, encoding it on the calling thread if no one did so yet. Returns null if the
    /// encode failed, the callback discarded the screenshot, or the pixels got released before.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="beforeEncode">
    /// Applied to the pixels right before the first encode. Returning false discards the screenshot.
    /// </param>
    internal byte[]? GetEncoded(SentryUnityOptions options, Func<SentryScreenshotPixels, bool>? beforeEncode = null)
    {
        var released = default(NativeArray<byte>);
        try
        {
            lock (_lock)
            {
                return EncodeLocked(options, beforeEncode, ref released);
            }
        }
        finally
        {
            if (released.IsCreated)
            {
                _returnBuffer(released);
            }
        }
    }

    // Must be called while holding _lock
    private byte[]? EncodeLocked(SentryUnityOptions options, Func<SentryScreenshotPixels, bool>? beforeEncode,
        ref NativeArray<byte> released)
    {
        if (_encodeStarted)
        {
            return _encoded;
        }

        _encodeStarted = true;

        if (_released)
        {
            return null;
        }

        try
        {
            if (beforeEncode is not null && !ApplyBeforeEncode(options, beforeEncode))
            {
                return null;
            }

            // Thread safe, so it runs wherever the attachment gets serialized
            using var encoded = ImageConversion.EncodeNativeArrayToJPG(_buffer, GraphicsFormat.R8G8B8A8_UNorm,
                (uint)Width, (uint)Height, 0, options.ScreenshotCompression);
            var bytes = encoded.ToArray();

            if (bytes is { Length: > 0 })
            {
                _encoded = bytes;
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
            released = ReleaseLocked();
        }

        return _encoded;
    }

    // Must be called while holding _lock
    private bool ApplyBeforeEncode(SentryUnityOptions options, Func<SentryScreenshotPixels, bool> beforeEncode)
    {
        try
        {
            if (beforeEncode(new SentryScreenshotPixels(_buffer, Width, Height)))
            {
                return true;
            }

            options.LogInfo("Screenshot discarded by BeforeSendScreenshotPixels callback.");
            return false;
        }
        catch (Exception e)
        {
            // The callback might have been about to redact something, so sending the screenshot as is isn't an option
            options.LogError(e, "The BeforeSendScreenshotPixels callback threw an exception. Discarding the screenshot.");
            return false;
        }
    }

    /// <summary>
    /// Drops the pixels, handing the buffer back for reuse. Later encodes return null.
    /// </summary>
    internal void ReleasePixels()
    {
        NativeArray<byte> released;
        lock (_lock)
        {
            released = ReleaseLocked();
        }

        if (released.IsCreated)
        {
            _returnBuffer(released);
        }
    }

    // Must be called while holding _lock
    private NativeArray<byte> ReleaseLocked()
    {
        if (_released)
        {
            return default;
        }

        _released = true;

        var buffer = _buffer;
        _buffer = default;
        return buffer;
    }
}
