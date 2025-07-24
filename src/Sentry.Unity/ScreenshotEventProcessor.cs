using System;
using System.Collections;
using Sentry.Extensibility;
using Sentry.Internal;
using UnityEngine;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessor
{
    private readonly SentryUnityOptions _options;
private volatile int _isCapturingScreenshot = 0;

// In Process method:
if (Interlocked.CompareExchange(ref _isCapturingScreenshot, 1, 0) == 0)
{
    _sentryMonoBehaviour.StartCoroutine(CaptureScreenshotCoroutine(@event.EventId));
}

    internal Func<SentryUnityOptions, byte[]> ScreenshotCaptureFunction = SentryScreenshot.Capture;
    internal Action<SentryId, SentryAttachment> AttachmentCaptureFunction = (eventId, attachment) =>
        ((Hub)Sentry.SentrySdk.CurrentHub).CaptureAttachment(eventId, attachment);
    internal Func<YieldInstruction> WaitForEndOfFrameFunction = () => new WaitForEndOfFrame();

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

        // WaitForEndOfFrame does not work in headless mode so we're making it configurable for CI.
        // See https://docs.unity3d.com/6000.1/Documentation/ScriptReference/WaitForEndOfFrame.html
        yield return WaitForEndOfFrameFunction();

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
