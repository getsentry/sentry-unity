using System;
using System.Collections;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Internal;
using UnityEngine;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessor
{
    private readonly SentryUnityOptions _options;
    private readonly ISentryMonoBehaviour _sentryMonoBehaviour;
    private volatile int _isCapturingScreenshot;

    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions) : this(sentryOptions, SentryMonoBehaviour.Instance) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, ISentryMonoBehaviour sentryMonoBehaviour)
    {
        _options = sentryOptions;
        _sentryMonoBehaviour = sentryMonoBehaviour;
    }

    public SentryEvent Process(SentryEvent @event)
    {
        // Only ever capture one screenshot per frame
        if (Interlocked.CompareExchange(ref _isCapturingScreenshot, 1, 0) == 0)
        {
            _options.LogDebug("Starting coroutine to capture a screenshot.");
            _sentryMonoBehaviour.QueueCoroutine(CaptureScreenshotCoroutine(@event.EventId));
        }

        return @event;
    }

    internal IEnumerator CaptureScreenshotCoroutine(SentryId eventId)
    {
        _options.LogDebug("Screenshot capture triggered. Waiting for End of Frame.");

        // WaitForEndOfFrame does not work in headless mode so we're making it configurable for CI.
        // See https://docs.unity3d.com/6000.1/Documentation/ScriptReference/WaitForEndOfFrame.html
        yield return WaitForEndOfFrame();

        try
        {
            if (_options.BeforeCaptureScreenshotInternal?.Invoke() is false)
            {
                yield break;
            }

            var screenshotBytes = CaptureScreenshot(_options);
            if (screenshotBytes.Length == 0)
            {
                _options.LogWarning("Screenshot capture returned empty data for event {0}", eventId);
                yield break;
            }

            var attachment = new SentryAttachment(
                    AttachmentType.Default,
                    new ByteAttachmentContent(screenshotBytes),
                    "screenshot.jpg",
                    "image/jpeg");

            _options.LogDebug("Screenshot captured for event {0}", eventId);

            CaptureAttachment(eventId, attachment);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to capture screenshot.");
        }
        finally
        {
            Interlocked.Exchange(ref _isCapturingScreenshot, 0);
        }
    }

    internal virtual byte[] CaptureScreenshot(SentryUnityOptions options)
        => SentryScreenshot.Capture(options);

    internal virtual void CaptureAttachment(SentryId eventId, SentryAttachment attachment)
        => (Sentry.SentrySdk.CurrentHub as Hub)?.CaptureAttachment(eventId, attachment);

    internal virtual YieldInstruction WaitForEndOfFrame()
        => new WaitForEndOfFrame();
}
