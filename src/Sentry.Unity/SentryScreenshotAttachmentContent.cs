using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Unity;

/// <summary>
/// Attachment content for a screenshot that gets taken at the end of the frame the event was captured in.
/// </summary>
/// <remarks>
/// The SDK reads attachment content when serializing the envelope on its background worker. Waiting for the screenshot
/// only then keeps it off the thread that captured the event, while the screenshot still goes out in the same envelope
/// as the event.
/// </remarks>
internal sealed class SentryScreenshotAttachmentContent : IAttachmentContent
{
    // How long the worker waits for the main thread to take the screenshot, e.g. while it's hanging or paused
    internal static readonly TimeSpan CaptureTimeout = TimeSpan.FromSeconds(1);

    private readonly Task<byte[]?> _screenshot;
    private readonly SentryUnityOptions _options;
    private readonly Stopwatch _sinceCreated = Stopwatch.StartNew();
    private volatile bool _isClaimed;

    internal SentryScreenshotAttachmentContent(Task<byte[]?> screenshot, SentryEvent @event, SentryUnityOptions options)
    {
        _screenshot = screenshot;
        Event = @event;
        _options = options;
    }

    internal SentryEvent Event { get; }

    // Whether the event made it past BeforeSend and sampling, i.e. the SDK built its envelope
    internal bool IsClaimed => _isClaimed;

    public Stream GetStream()
    {
        _isClaimed = true;
        return new WaitOnReadStream(this);
    }

    internal async Task<byte[]> GetEncodedAsync()
    {
        if (!_screenshot.IsCompleted)
        {
            // Taking the screenshot needs the main thread to reach the end of the frame. Waiting on it there would deadlock.
            if (MainThreadData.IsMainThread())
            {
                _options.LogWarning("Screenshot not taken yet and the attachment is read on the main thread. Skipping.");
                return Array.Empty<byte>();
            }

            // A deadline, so envelopes sharing a screenshot that never comes don't each wait the full timeout
            var remaining = CaptureTimeout - _sinceCreated.Elapsed;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            if (await Task.WhenAny(_screenshot, Task.Delay(remaining)).ConfigureAwait(false) != _screenshot)
            {
                _options.LogWarning("Screenshot was not taken in time. Skipping.");
                return Array.Empty<byte>();
            }
        }

        var bytes = await _screenshot.ConfigureAwait(false);
        return bytes is { Length: > 0 } ? bytes : Array.Empty<byte>();
    }

    // Deliberately not seekable: the SDK checks the length of attachment streams when building the envelope, on the
    // capturing thread. Reporting no length keeps the wait from happening right there.
    private sealed class WaitOnReadStream : Stream
    {
        private readonly SentryScreenshotAttachmentContent _content;
        private MemoryStream? _encoded;

        public WaitOnReadStream(SentryScreenshotAttachmentContent content) => _content = content;

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
            _encoded ??= new MemoryStream(_content.GetEncodedAsync().GetAwaiter().GetResult(), writable: false);
            return _encoded.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _encoded ??= new MemoryStream(await _content.GetEncodedAsync().ConfigureAwait(false), writable: false);
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
