using System;
using System.IO;
using Sentry;
using Sentry.Extensibility;
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
            // Captures the current screenshot synchronously (throws if not on the UI thread - sentry-dotnet skips the attachment in that case)
            var texture = ScreenCapture.CaptureScreenshotAsTexture();

            // resize if needed
            var ratioH = _options.ScreenshotMaxHeight <= 0 ? 1.0f : (float)_options.ScreenshotMaxHeight / (float)texture.height;
            var ratioW = _options.ScreenshotMaxWidth <= 0 ? 1.0f : (float)_options.ScreenshotMaxWidth / (float)texture.width;
            var ratio = Mathf.Min(ratioH, ratioW);
            if (ratio > 0.0f && ratio < 1.0f)
            {
                texture.Resize(Mathf.FloorToInt((float)texture.width * ratio), Mathf.FloorToInt((float)texture.height * ratio));
                texture.Apply();
                _options.DiagnosticLogger?.LogDebug("Screenshot resized to {0} %", ratio * 100);
            }

            var bytes = texture.EncodeToJPG(_options.ScreenshotQuality);
            _options.DiagnosticLogger?.LogDebug("Screenshot captured: {0} bytes", bytes.Length);
            return new MemoryStream(bytes);
        }
    }
}
