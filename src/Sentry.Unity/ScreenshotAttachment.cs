using System;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    internal class ScreenshotAttachment : Attachment
    {
        public ScreenshotAttachment(IAttachmentContent content)
            : base(AttachmentType.Default, content, "screenshot.jpg", "image/jpeg") { }
    }

    internal class ScreenshotAttachmentContent : IAttachmentContent
    {
        private readonly SentryMonoBehaviour _behaviour;
        private readonly SentryUnityOptions _options;

        public ScreenshotAttachmentContent(SentryUnityOptions options, SentryMonoBehaviour behaviour)
        {
            _behaviour = behaviour;
            _options = options;
        }

        public Stream GetStream()
        {
            // Note: we need to check explicitly that we're on the same thread. While Unity would throw otherwise
            // when capturing the screenshot, it would only do so on development builds. On release, it just crashes...
            if (!_behaviour.MainThreadData.IsMainThread())
            {
                _options.DiagnosticLogger?.LogDebug("Won't capture screenshot because we're not on the main thread");
                // Throwing here to avoid empty attachment being sent to Sentry.
                // return new MemoryStream();
                throw new Exception("Sentry: cannot capture screenshot attachment on other than the main (UI) thread.");
            }

            return new MemoryStream(CaptureScreenshot());
        }

        private int GetTargetResolution(ScreenshotQuality quality)
        {
            return quality switch
            {
                ScreenshotQuality.High => 1920,     // 1080p
                ScreenshotQuality.Medium => 1280,   // 720p
                ScreenshotQuality.Low => 854,       // 480p
                _ => 854                            // Fallback
            };
        }

        private byte[] CaptureScreenshot()
        {
            var width = Screen.width;
            var height = Screen.height;

            // Make sure the screenshot size does not exceed the target size by scaling the image while conserving the
            // original ratio based on which, width or height, is the smaller
            if (_options.ScreenshotQuality is not ScreenshotQuality.Full)
            {
                var targetResolution = GetTargetResolution(_options.ScreenshotQuality);
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
            var rtFull = RenderTexture.GetTemporary(Screen.width, Screen.height);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rtFull);
            var rtResized = RenderTexture.GetTemporary(width, height);
            // On all (currently supported) platforms except Android, the image is mirrored horizontally & vertically.
            // So we must mirror it back.
            if (ApplicationAdapter.Instance.Platform is (RuntimePlatform.Android or RuntimePlatform.LinuxPlayer))
            {
                Graphics.Blit(rtFull, rtResized);
            }
            else
            {
                Graphics.Blit(rtFull, rtResized, new Vector2(1, -1), new Vector2(0, 1));
            }
            RenderTexture.ReleaseTemporary(rtFull);
            // Remember the previous render target and change it to our target texture.
            var previousRT = RenderTexture.active;
            RenderTexture.active = rtResized;
            try
            {
                // actually copy from the current render target a texture & read data from the active RenderTexture
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();
            }
            finally
            {
                // Restore the render target.
                RenderTexture.active = previousRT;
            }

            var bytes = screenshot.EncodeToJPG(_options.ScreenshotCompression);
            _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Screenshot captured at {0}x{1}: {2} bytes", null, width, height, bytes.Length);
            return bytes;
        }
    }
}
