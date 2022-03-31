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
        private readonly SentryOptions _options;

        public ScreenshotAttachmentContent(SentryOptions options, SentryMonoBehaviour behaviour)
        {
            _behaviour = behaviour;
            _options = options;
        }

        public Stream GetStream()
        {
            if (!_behaviour.MainThreadData.IsMainThread())
            {
                _options.DiagnosticLogger?.LogDebug("Won't capture screenshot because we're not on the main thread");
                return new MemoryStream();
            }
            else
            {
                // Captures current screenshot synchronously
                try
                {
                    var texture = ScreenCapture.CaptureScreenshotAsTexture();
                    var bytes = texture.EncodeToJPG();
                    _options.DiagnosticLogger?.LogDebug("Screenshot captured: {0} bytes", bytes.Length);
                    return new MemoryStream(bytes);
                }
                catch (Exception ex)
                {
                    _options.DiagnosticLogger?.LogError("Couldn't capture screenshot", ex);
                }
            }
            return new MemoryStream();
        }
    }
}
