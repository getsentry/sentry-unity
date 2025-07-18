using System;
using System.Collections;
using Sentry.Extensibility;
using Sentry.Internal;
using UnityEngine;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessor
{
    private readonly SentryUnityOptions _options;
    private readonly ISentryMonoBehaviour _sentryMonoBehaviour;
    private bool _isCapturingScreenshot;

    internal Func<SentryUnityOptions, byte[]> ScreenshotCaptureFunction = SentryScreenshot.Capture;
    internal Action<SentryId, SentryAttachment> AttachmentCaptureFunction = (eventId, attachment) =>
        ((Hub)Sentry.SentrySdk.CurrentHub).CaptureAttachment(eventId, attachment);

    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions) : this(sentryOptions, SentryMonoBehaviour.Instance) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, ISentryMonoBehaviour sentryMonoBehaviour)
    {
        _options = sentryOptions;
        _sentryMonoBehaviour = sentryMonoBehaviour;
    }

    public SentryEvent Process(SentryEvent @event)
    {
        // Only ever capture one screenshot per frame
        if (!_isCapturingScreenshot)
        {
            _isCapturingScreenshot = true;
            _sentryMonoBehaviour.StartCoroutine(CaptureScreenshotCoroutine(@event.EventId));
        }
        return @event;
    }

    internal IEnumerator CaptureScreenshotCoroutine(SentryId eventId)
    {
        _options.LogDebug("Screenshot capture triggered. Waiting for End of Frame.");

        yield return new WaitForEndOfFrame();

        try
        {
            var screenshotBytes = ScreenshotCaptureFunction(_options);
            var attachment = new SentryAttachment(
                    AttachmentType.Default,
                    new ByteAttachmentContent(screenshotBytes),
                    "screenshot.jpg",
                    "image/jpeg");

            _options.LogDebug("Screenshot captured for event {0}", eventId);

            AttachmentCaptureFunction(eventId, attachment);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to capture screenshot.");
        }
        finally
        {
            _isCapturingScreenshot = false;
        }
    }
}
