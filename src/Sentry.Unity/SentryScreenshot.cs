using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sentry.Unity;

internal static class SentryScreenshot
{
    internal static int GetTargetResolution(ScreenshotQuality quality)
    {
        return quality switch
        {
            ScreenshotQuality.High => 1920,     // 1080p
            ScreenshotQuality.Medium => 1280,   // 720p
            ScreenshotQuality.Low => 854,       // 480p
            _ => 854                            // Fallback
        };
    }

    public static Texture2D CreateNewScreenshotTexture2D(SentryUnityOptions options) =>
        CreateNewScreenshotTexture2D(options, Screen.width, Screen.height);

    // For testing
    internal static Texture2D CreateNewScreenshotTexture2D(SentryUnityOptions options, int width, int height)
    {
        var renderTexture = CaptureIntoRenderTexture(options, width, height);
        var previousRenderTexture = RenderTexture.active;

        try
        {
            // Captures the current screenshot synchronously.
            var screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            screenshot.Apply();

            options.LogDebug("Screenshot captured at {0}x{1}.", renderTexture.width, renderTexture.height);

            return screenshot;
        }
        finally
        {
            RenderTexture.active = previousRenderTexture;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    /// <summary>
    /// Reads the current screenshot back from the GPU and encodes it to JPG on a background thread, without stalling
    /// the main thread.
    /// </summary>
    internal static void CaptureAsync(SentryUnityOptions options, Action<byte[]?> onCaptured)
    {
        var renderTexture = CaptureIntoRenderTexture(options, Screen.width, Screen.height);
        var (width, height) = ((uint)renderTexture.width, (uint)renderTexture.height);
        var format = renderTexture.graphicsFormat;

        try
        {
            // The texture has to stay out of the pool until the readback is done with it. Reading it back in its own
            // format, as converting fails for formats the platform can't ReadPixels from, e.g. BGRA on Metal.
            AsyncGPUReadback.Request(renderTexture, 0, readback =>
            {
                RenderTexture.ReleaseTemporary(renderTexture);

                if (readback.hasError)
                {
                    options.LogError("Failed to read back the screenshot.");
                    onCaptured(null);
                    return;
                }

                var pixels = readback.GetData<byte>().ToArray();
                Task.Run(() =>
                {
                    byte[]? bytes = null;
                    try
                    {
                        bytes = ImageConversion.EncodeArrayToJPG(pixels, format, width, height,
                            0, options.ScreenshotCompression);
                    }
                    catch (Exception e)
                    {
                        options.LogError(e, "Failed to encode the screenshot.");
                    }

                    onCaptured(bytes);
                });
            });
        }
        catch
        {
            RenderTexture.ReleaseTemporary(renderTexture);
            throw;
        }
    }

    /// <summary>
    /// Copies a screen captured with <see cref="ScreenCapture.CaptureScreenshotIntoRenderTexture(RenderTexture)"/>
    /// into <paramref name="destination"/>, scaling it to fit. The rows start at the bottom, as in Unity's textures,
    /// or with <paramref name="topDown"/> at the top, as in Android's bitmaps.
    /// </summary>
    internal static void BlitScreen(RenderTexture screen, RenderTexture destination, bool topDown = false)
    {
        // The captured image is mirrored on platforms where UVs start at the top.
        // See https://docs.unity3d.com/2019.4/Documentation/Manual/SL-PlatformDifferences.html for more info.
        // Note, we can't use the `UNITY_UV_STARTS_AT_TOP` macro because it's only available in shaders.
        // Instead, there's https://docs.unity3d.com/2019.4/Documentation/ScriptReference/SystemInfo-graphicsUVStartsAtTop.html
        var uvStartsAtTop = SentrySystemInfoAdapter.Instance.GraphicsUVStartsAtTop ?? true;
        if (uvStartsAtTop != topDown)
        {
            Graphics.Blit(screen, destination, new Vector2(1, -1), new Vector2(0, 1));
        }
        else
        {
            Graphics.Blit(screen, destination);
        }
    }

    // Returns a temporary render texture holding the screenshot, scaled down to the configured quality
    private static RenderTexture CaptureIntoRenderTexture(SentryUnityOptions options, int width, int height)
    {
        // Make sure the screenshot size does not exceed the target size by scaling the image while conserving the
        // original ratio based on which, width or height, is the smaller
        if (options.ScreenshotQuality is not ScreenshotQuality.Full)
        {
            var targetResolution = GetTargetResolution(options.ScreenshotQuality);
            var ratioW = targetResolution / (float)width;
            var ratioH = targetResolution / (float)height;
            var ratio = Mathf.Min(ratioH, ratioW);
            if (ratio is > 0.0f and < 1.0f)
            {
                width = Mathf.FloorToInt(width * ratio);
                height = Mathf.FloorToInt(height * ratio);
            }
        }

        // Blit changes the active render target
        var previousRenderTexture = RenderTexture.active;
        var renderTextureFull = RenderTexture.GetTemporary(Screen.width, Screen.height);
        try
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTextureFull);
            var renderTextureResized = RenderTexture.GetTemporary(width, height);
            BlitScreen(renderTextureFull, renderTextureResized);
            return renderTextureResized;
        }
        finally
        {
            RenderTexture.active = previousRenderTexture;
            RenderTexture.ReleaseTemporary(renderTextureFull);
        }
    }
}
