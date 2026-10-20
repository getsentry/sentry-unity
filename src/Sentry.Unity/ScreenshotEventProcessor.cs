using System;
using Sentry.Extensibility;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessorWithHint
{
    private readonly SentryUnityOptions _options;
    private readonly SentryScreenshotCapturer _capturer;

    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions)
        : this(sentryOptions, GetOrCreateCapturer(sentryOptions)) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, SentryScreenshotCapturer capturer)
    {
        _options = sentryOptions;
        _capturer = capturer;
    }

    private static SentryScreenshotCapturer GetOrCreateCapturer(SentryUnityOptions options)
    {
        if (options.ScreenshotCapturer is { } capturer)
        {
            return capturer;
        }

        capturer = new SentryScreenshotCapturer(options);
        SentryMonoBehaviour.Instance.StartScreenshotCapturer(capturer);
        options.ScreenshotCapturer = capturer;
        return capturer;
    }

    public SentryEvent? Process(SentryEvent @event) => @event;

    public SentryEvent? Process(SentryEvent @event, SentryHint hint)
    {
        // Feedback attaches a screenshot only when asked to, see SentryUnitySdk.CaptureFeedback
        if (@event.Contexts.Feedback is not null)
        {
            return @event;
        }

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

        // The screenshot gets taken at the end of this frame. A callback to its pixels gets applied on the SDK's worker
        // right before encoding. The obsolete Texture2D callback ties applying it - and encoding - to the main thread.
        var content = _options.BeforeSendScreenshotPixelsInternal is { } beforeSendPixels
            ? _capturer.TryRequest(beforeEncode: pixels => beforeSendPixels(pixels, @event))
            : _options.BeforeSendScreenshotInternal is { } beforeSend
                ? _capturer.TryRequest(screenshot => beforeSend(screenshot, @event))
                : _capturer.TryRequest();

        if (content is null)
        {
            return @event;
        }

        hint.Attachments.Add(new SentryAttachment(AttachmentType.Default, content, "screenshot.jpg", "image/jpeg"));
        _options.LogDebug("Screenshot requested for event {0}", @event.EventId);

        return @event;
    }
}
