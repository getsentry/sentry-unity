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
        private readonly int[] _resolutionModifiers = { 1, 2, 3, 4 }; // Full, half, third, quarter

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
                _options.DiagnosticLogger?.LogDebug("Can't capture screenshots on other than main (UI) thread.");
                return new MemoryStream();
            }

            return new MemoryStream(CaptureScreenshot());
        }

        private int GetResolutionModifier(ScreenshotQuality quality)
        {
            var index = (int)quality;
            if (index < _resolutionModifiers.Length)
            {
                return _resolutionModifiers[index];
            }

            return 1;
        }

        private byte[] CaptureScreenshot()
        {
            var resolutionModifier = GetResolutionModifier(_options.ScreenshotQuality);

            // Make sure the screenshot size does not exceed MaxSize by scaling the image while conserving the
            // original ratio based on which, width or height, is the smaller
            var targetWidth = Screen.width / resolutionModifier;
            var targetHeight = Screen.height / resolutionModifier;

            // Captures the current screenshot synchronously.
            var screenshot = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            var rtFull = RenderTexture.GetTemporary(Screen.width, Screen.height);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rtFull);
            var rtResized = RenderTexture.GetTemporary(targetWidth, targetHeight);
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
                screenshot.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                screenshot.Apply();
            }
            finally
            {
                // Restore the render target.
                RenderTexture.active = previousRT;
            }

            var bytes = screenshot.EncodeToJPG(_options.ScreenshotCompression);
            _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Screenshot captured at {0}x{1}: {2} bytes", null, targetWidth, targetHeight, bytes.Length);
            return bytes;
        }
    }
}
