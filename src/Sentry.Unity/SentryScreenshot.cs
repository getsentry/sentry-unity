using System;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
    /// Reads the current screenshot back from the GPU without stalling the main thread. The result gets encoded to JPG
    /// on first access, on the accessing thread.
    /// </summary>
    internal static void CaptureAsync(SentryUnityOptions options, Action<Lazy<byte[]?>?> onCaptured)
    {
        var renderTexture = CaptureIntoRenderTexture(options, Screen.width, Screen.height);
        var (width, height) = ((uint)renderTexture.width, (uint)renderTexture.height);

        try
        {
            // The readback copies the texture as of now, so it can go back to the pool right away
            AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGBA32, readback =>
            {
                if (readback.hasError)
                {
                    options.LogError("Failed to read back the screenshot.");
                    onCaptured(null);
                    return;
                }

                var pixels = readback.GetData<byte>().ToArray();
                onCaptured(new Lazy<byte[]?>(() =>
                    ImageConversion.EncodeArrayToJPG(pixels, GraphicsFormat.R8G8B8A8_UNorm, width, height, 0,
                        options.ScreenshotCompression)));
            });
        }
        finally
        {
            RenderTexture.ReleaseTemporary(renderTexture);
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

        var renderTextureFull = RenderTexture.GetTemporary(Screen.width, Screen.height);
        try
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTextureFull);
            var renderTextureResized = RenderTexture.GetTemporary(width, height);

            // The image may be mirrored on some platforms - mirror it back.
            // See https://docs.unity3d.com/2019.4/Documentation/Manual/SL-PlatformDifferences.html for more info.
            // Note, we can't use the `UNITY_UV_STARTS_AT_TOP` macro because it's only available in shaders.
            // Instead, there's https://docs.unity3d.com/2019.4/Documentation/ScriptReference/SystemInfo-graphicsUVStartsAtTop.html
            if (SentrySystemInfoAdapter.Instance.GraphicsUVStartsAtTop ?? true)
            {
                Graphics.Blit(renderTextureFull, renderTextureResized, new Vector2(1, -1), new Vector2(0, 1));
            }
            else
            {
                Graphics.Blit(renderTextureFull, renderTextureResized);
            }

            return renderTextureResized;
        }
        finally
        {
            RenderTexture.ReleaseTemporary(renderTextureFull);
        }
    }
}
