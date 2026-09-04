using System;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Holds the most recently completed frame so that it can be attached to an event synchronously,
/// from within the event processor pipeline, instead of being sent as a separate envelope.
/// </summary>
internal class SentryScreenshotCache : IDisposable
{
    private readonly SentryUnityOptions _options;

    private RenderTexture? _cache;
    private int _width;
    private int _height;

    private int _generation;
    private byte[]? _encoded;
    private int _encodedGeneration = -1;

    internal SentryScreenshotCache(SentryUnityOptions options)
    {
        _options = options;
    }

    internal bool HasContent => _generation > 0;

    /// <summary>
    /// Captures the current back buffer into the cache. Must run on the main thread at end of frame -
    /// capturing mid-frame yields an incomplete image.
    /// </summary>
    internal virtual void Refresh()
    {
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        var (targetWidth, targetHeight) = SentryScreenshot.GetTargetSize(_options.ScreenshotQuality, screenWidth, screenHeight);
        EnsureCache(targetWidth, targetHeight);

        var previous = RenderTexture.active;
        try
        {
            // Capturing straight into the downscaled cache keeps this - the only recurring cost of
            // attaching screenshots - to a single operation. Mirroring is handled on read instead.
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_cache);
            _generation++;
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

    /// <summary>
    /// Encodes the cached frame as JPG. Must run on the main thread. Returns null when the cache has
    /// not been populated yet or the transform discarded the screenshot.
    /// </summary>
    internal virtual byte[]? TryEncodeLatest(Func<Texture2D, Texture2D?>? transform = null)
    {
        if (!HasContent)
        {
            _options.LogDebug("Screenshot cache is empty. Skipping.");
            return null;
        }

        // Every event between two refreshes sees the same frame, so the encode is only worth doing once.
        // A transform may return a different image per event, which makes the cached bytes unusable.
        if (transform is null && _encodedGeneration == _generation)
        {
            return _encoded;
        }

        Texture2D? screenshot = null;
        try
        {
            screenshot = ReadCache();

            if (transform is not null)
            {
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
            }

            var bytes = screenshot.EncodeToJPG(_options.ScreenshotCompression);
            if (bytes is null || bytes.Length == 0)
            {
                _options.LogWarning("Screenshot encoding returned empty data.");
                return null;
            }

            if (transform is null)
            {
                _encoded = bytes;
                _encodedGeneration = _generation;
            }

            return bytes;
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to encode the cached screenshot.");
            return null;
        }
        finally
        {
            if (screenshot != null)
            {
                UnityEngine.Object.Destroy(screenshot);
            }
        }
    }

    private Texture2D ReadCache()
    {
        var previous = RenderTexture.active;
        RenderTexture? mirrored = null;
        try
        {
            var source = _cache!;

            // The image may be mirrored on some platforms - mirror it back.
            // See https://docs.unity3d.com/2019.4/Documentation/Manual/SL-PlatformDifferences.html for more info.
            // Note, we can't use the `UNITY_UV_STARTS_AT_TOP` macro because it's only available in shaders.
            if (SentrySystemInfoAdapter.Instance.GraphicsUVStartsAtTop ?? true)
            {
                mirrored = RenderTexture.GetTemporary(_width, _height);
                Graphics.Blit(source, mirrored, new Vector2(1, -1), new Vector2(0, 1));
                source = mirrored;
            }

            RenderTexture.active = source;
            var screenshot = new Texture2D(_width, _height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            screenshot.Apply();
            return screenshot;
        }
        finally
        {
            RenderTexture.active = previous;

            if (mirrored)
            {
                RenderTexture.ReleaseTemporary(mirrored);
            }
        }
    }

    private void EnsureCache(int width, int height)
    {
        if (_cache != null && _width == width && _height == height)
        {
            return;
        }

        ReleaseCache();

        _cache = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { name = "Sentry.ScreenshotCache" };
        _cache.Create();
        _width = width;
        _height = height;

        // The previous frame is gone and the new texture is uninitialized.
        _generation = 0;
        _encodedGeneration = -1;
        _encoded = null;

        _options.LogDebug("Screenshot cache allocated at {0}x{1}.", width, height);
    }

    private void ReleaseCache()
    {
        if (_cache == null)
        {
            return;
        }

        _cache.Release();
        UnityEngine.Object.Destroy(_cache);
        _cache = null;
    }

    public void Dispose()
    {
        ReleaseCache();
        _encoded = null;
        _encodedGeneration = -1;
        _generation = 0;
    }
}
