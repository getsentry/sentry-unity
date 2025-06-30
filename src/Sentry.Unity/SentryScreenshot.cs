using UnityEngine;

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

    public static byte[] Capture(SentryUnityOptions options) =>
        Capture(options, Screen.width, Screen.height);

    // For testing
    internal static byte[] Capture(SentryUnityOptions options, int width, int height)
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

        Texture2D? screenshot = null;
        RenderTexture? renderTextureFull = null;
        RenderTexture? renderTextureResized = null;
        var previousRenderTexture = RenderTexture.active;

        try
        {
            // Captures the current screenshot synchronously.
            screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            renderTextureFull = RenderTexture.GetTemporary(Screen.width, Screen.height);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTextureFull);
            renderTextureResized = RenderTexture.GetTemporary(width, height);

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

            RenderTexture.active = renderTextureResized;
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            var bytes = screenshot.EncodeToJPG(options.ScreenshotCompression);

            options.DiagnosticLogger?.Log(SentryLevel.Debug,
                "Screenshot captured at {0}x{1}: {2} bytes", null, width, height, bytes.Length);

            return bytes;
        }
        finally
        {
            RenderTexture.active = previousRenderTexture;

            if (renderTextureFull)
            {
                RenderTexture.ReleaseTemporary(renderTextureFull);
            }

            if (renderTextureResized)
            {
                RenderTexture.ReleaseTemporary(renderTextureResized);
            }

            if (screenshot)
            {
                Object.Destroy(screenshot);
            }
        }
    }
}
