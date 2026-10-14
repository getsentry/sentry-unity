using System;
using System.IO;
using Sentry.Extensibility;

namespace Sentry.Unity;

/// <summary>
/// Attachment content that encodes the screenshot only once it gets read.
/// </summary>
/// <remarks>
/// The SDK reads attachment content when serializing the envelope on its background worker. Deferring the encode
/// until then keeps it off the thread that captured the event while the screenshot still goes out in the same
/// envelope as the event. Events that get dropped before being sent - e.g. sampled out - never pay for the encode.
/// </remarks>
internal sealed class SentryScreenshotAttachmentContent : IAttachmentContent
{
    private readonly SentryScreenshotFrame _frame;
    private readonly SentryUnityOptions _options;

    internal SentryScreenshotAttachmentContent(SentryScreenshotFrame frame, SentryUnityOptions options)
    {
        _frame = frame;
        _options = options;
    }

    internal SentryScreenshotFrame Frame => _frame;

    public Stream GetStream() => new EncodeOnReadStream(this);

    private byte[] Encode()
    {
        var bytes = _frame.GetEncoded(_options);
        if (bytes is null)
        {
            _options.LogWarning("Screenshot attachment is empty. Encoding the screenshot failed.");
        }

        return bytes ?? Array.Empty<byte>();
    }

    // Deliberately not seekable: the SDK checks the length of attachment streams when building the envelope, on the
    // capturing thread. Reporting no length keeps the encode from happening right there.
    private sealed class EncodeOnReadStream : Stream
    {
        private readonly SentryScreenshotAttachmentContent _content;
        private MemoryStream? _encoded;

        public EncodeOnReadStream(SentryScreenshotAttachmentContent content) => _content = content;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _encoded ??= new MemoryStream(_content.Encode(), writable: false);
            return _encoded.Read(buffer, offset, count);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _encoded?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
