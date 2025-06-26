using UnityEngine;

namespace Sentry.Unity;

internal static class SentryScreenshotUtility
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

    public static byte[] Capture(SentryUnityOptions options)
    {
        var width = Screen.width;
        var height = Screen.height;

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

        // Captures the current screenshot synchronously.
        var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        var renderTextureFull = RenderTexture.GetTemporary(Screen.width, Screen.height);
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
        RenderTexture.ReleaseTemporary(renderTextureFull);
        // Remember the previous render target and change it to our target texture.
        var previousRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTextureResized;

        try
        {
            // actually copy from the current render target a texture & read data from the active RenderTexture
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
        }
        finally
        {
            // Restore the render target.
            RenderTexture.active = previousRenderTexture;
        }

        RenderTexture.ReleaseTemporary(renderTextureResized);

        var bytes = screenshot.EncodeToJPG(options.ScreenshotCompression);
        Object.Destroy(screenshot);

        options.DiagnosticLogger?.Log(SentryLevel.Debug,
            "Screenshot captured at {0}x{1}: {2} bytes", null, width, height, bytes.Length);

        return bytes;
    }
}
