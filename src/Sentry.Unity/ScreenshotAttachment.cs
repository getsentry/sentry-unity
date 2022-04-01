using System;
using System.IO;
using Sentry;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;
using UnityEngine.Rendering;

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
            // Calculate the desired size by calculating the ratio between the desired height/width and the actual one,
            // and than resizing based on the smaller of the two ratios.
            var width = Screen.width;
            var height = Screen.height;
            var ratioW = _options.ScreenshotMaxWidth <= 0 ? 1.0f : (float)_options.ScreenshotMaxWidth / (float)width;
            var ratioH = _options.ScreenshotMaxHeight <= 0 ? 1.0f : (float)_options.ScreenshotMaxHeight / (float)height;
            var ratio = Mathf.Min(ratioH, ratioW);
            if (ratio > 0.0f && ratio < 1.0f)
            {
                width = Mathf.FloorToInt((float)width * ratio);
                height = Mathf.FloorToInt((float)height * ratio);
            }

            // Captures the current screenshot synchronously.
            // Throws if not on the UI thread - sentry-dotnet skips the attachment in that case.
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            var rtFull = RenderTexture.GetTemporary(Screen.width, Screen.height);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rtFull);
            var rtResized = RenderTexture.GetTemporary(width, height);
            // On all (currently supported) platforms except Android, the image is mirrored horizontally & vertically.
            // So we must mirror it back.
            if (ApplicationAdapter.Instance.Platform == RuntimePlatform.Android)
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

            var bytes = screenshot.EncodeToJPG(_options.ScreenshotQuality);
            _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Screenshot captured at {0}x{1}: {0} bytes", null, width, height, bytes.Length);
            return new MemoryStream(bytes);
        }
    }
}
