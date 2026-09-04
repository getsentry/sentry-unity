using Sentry.Extensibility;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessorWithHint
{
    private readonly SentryUnityOptions _options;
    private readonly SentryScreenshotCache _cache;

    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions)
        : this(sentryOptions, GetOrCreateCache(sentryOptions)) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, SentryScreenshotCache cache)
    {
        _options = sentryOptions;
        _cache = cache;
    }

    private static SentryScreenshotCache GetOrCreateCache(SentryUnityOptions options)
    {
        if (options.ScreenshotCache is { } cache)
        {
            return cache;
        }

        cache = new SentryScreenshotCache(options);
        SentryMonoBehaviour.Instance.StartScreenshotCache(cache, options.ScreenshotCaptureInterval);
        options.ScreenshotCache = cache;
        return cache;
    }

    public SentryEvent? Process(SentryEvent @event) => @event;

    public SentryEvent? Process(SentryEvent @event, SentryHint hint)
    {
        // Reading the cached frame back off the GPU is a main thread only operation.
        if (!MainThreadData.IsMainThread())
        {
            _options.LogDebug("Screenshot capture skipped. Can't capture screenshots on other than the main thread.");
            return @event;
        }

        if (_options.BeforeCaptureScreenshotInternal?.Invoke(@event) is false)
        {
            _options.LogInfo("Screenshot capture skipped by BeforeCaptureScreenshot callback.");
            return @event;
        }

        var beforeSend = _options.BeforeSendScreenshotInternal;
        var screenshotBytes = _cache.TryEncodeLatest(beforeSend is null
            ? null
            : screenshot => beforeSend(screenshot, @event));

        if (screenshotBytes is null)
        {
            return @event;
        }

        hint.AddAttachment(screenshotBytes, "screenshot.jpg", AttachmentType.Default, "image/jpeg");
        _options.LogDebug("Screenshot attached to event {0}", @event.EventId);

        return @event;
    }
}
