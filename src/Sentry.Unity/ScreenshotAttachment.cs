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
                // The async version - uses a custom stream & captures in a coroutine
                // return new ScreenshotCaptureStream(_options, _behaviour);

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

    // The async version, currently unused because we want the screenshot to be as early as possible after the error
    // internal class ScreenshotCaptureStream : Stream
    // {
    //     private readonly SentryOptions _options;
    //     private readonly TaskCompletionSource<byte[]> _tcs;

    //     public ScreenshotCaptureStream(SentryOptions options, SentryMonoBehaviour behaviour)
    //     {
    //         _options = options;
    //         _tcs = new();
    //         behaviour.StartCoroutine(CaptureScreenshot());
    //     }

    //     private IEnumerator CaptureScreenshot()
    //     {
    //         _options.DiagnosticLogger?.LogDebug("Capturing screenshot");

    //         yield return new WaitForEndOfFrame();

    //         var texture = ScreenCapture.CaptureScreenshotAsTexture();
    //         byte[] bytes = texture.EncodeToJPG();
    //         UnityEngine.Object.Destroy(texture);

    //         _options.DiagnosticLogger?.LogDebug("Screenshot captured: {0} bytes", bytes.Length);

    //         _tcs.TrySetResult(bytes);
    //     }

    //     public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    //     {
    //         var bytes = await _tcs.Task.ConfigureAwait(false);
    //         _options.DiagnosticLogger?.LogDebug("Awaiting screenshot finished: {0} bytes", bytes.Length);
    //         var writer = new BinaryWriter(destination);
    //         {
    //             writer.Write(bytes);
    //         }
    //         _options.DiagnosticLogger?.LogDebug("Finished writing the screenshot");
    //     }

    //     public override bool CanRead => throw new NotImplementedException();
    //     public override bool CanSeek => throw new NotImplementedException();
    //     public override bool CanWrite => throw new NotImplementedException();
    //     public override long Length => throw new NotImplementedException();
    //     public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //     public override void Flush() => throw new NotImplementedException();
    //     public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    //     public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
    //     public override void SetLength(long value) => throw new NotImplementedException();
    //     public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    // }
}
