using System;
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
        // A processor throwing drops the whole event, so a failing callback must only cost the screenshot.
        try
        {
            if (_options.BeforeCaptureScreenshotInternal?.Invoke(@event) is false)
            {
                _options.LogInfo("Screenshot capture skipped by BeforeCaptureScreenshot callback.");
                return @event;
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "The BeforeCaptureScreenshot callback threw an exception. Skipping the screenshot.");
            return @event;
        }

        // Without a callback to apply, the encode gets deferred until the envelope is serialized on the SDK's
        // background worker. The callback operates on a Texture2D, which ties it to encoding right away.
        var beforeSend = _options.BeforeSendScreenshotInternal;
        var content = beforeSend is null
            ? _cache.TryGetLatestContent()
            : _cache.TryEncodeLatest(screenshot => beforeSend(screenshot, @event)) is { } bytes
                ? new ByteAttachmentContent(bytes)
                : null;

        if (content is null)
        {
            return @event;
        }

        hint.Attachments.Add(new SentryAttachment(AttachmentType.Default, content, "screenshot.jpg", "image/jpeg"));
        _options.LogDebug("Screenshot attached to event {0}", @event.EventId);

        return @event;
    }
}
