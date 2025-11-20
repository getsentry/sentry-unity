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
            _sentryMonoBehaviour.QueueCoroutine(CaptureScreenshotCoroutine(@event));
        }

        return @event;
    }

    internal IEnumerator CaptureScreenshotCoroutine(SentryEvent @event)
    {
        _options.LogDebug("Screenshot capture triggered. Waiting for End of Frame.");

        // WaitForEndOfFrame does not work in headless mode so we're making it configurable for CI.
        // See https://docs.unity3d.com/6000.1/Documentation/ScriptReference/WaitForEndOfFrame.html
        yield return WaitForEndOfFrame();

        Texture2D? screenshot = null;
        try
        {
            if (_options.BeforeCaptureScreenshotInternal?.Invoke() is false)
            {
                yield break;
            }

            screenshot = CreateNewScreenshotTexture2D(_options);

            if (_options.BeforeSendScreenshotInternal != null)
            {
                var modifiedScreenshot = _options.BeforeSendScreenshotInternal(screenshot, @event);

                if (modifiedScreenshot == null)
                {
                    _options.LogInfo("Screenshot discarded by BeforeSendScreenshot callback.");
                    yield break;
                }

                // Clean up - If the user returned a new texture object and did not modify the passed in one
                if (modifiedScreenshot != screenshot)
                {
                    _options.LogDebug("Applying modified screenshot.");
                    UnityEngine.Object.Destroy(screenshot);
                    screenshot = modifiedScreenshot;
                }
            }

            var screenshotBytes = screenshot.EncodeToJPG(_options.ScreenshotCompression);
            if (screenshotBytes is null || screenshotBytes.Length == 0)
            {
                _options.LogWarning("Screenshot capture returned empty data for event {0}", @event.EventId);
                yield break;
            }

            var attachment = new SentryAttachment(
                    AttachmentType.Default,
                    new ByteAttachmentContent(screenshotBytes),
                    "screenshot.jpg",
                    "image/jpeg");

            _options.LogDebug("Screenshot captured for event {0}", @event.EventId);

            CaptureAttachment(@event.EventId, attachment);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to capture screenshot.");
        }
        finally
        {
            Interlocked.Exchange(ref _isCapturingScreenshot, 0);

            if (screenshot != null)
            {
                UnityEngine.Object.Destroy(screenshot);
            }
        }
    }

    internal virtual Texture2D CreateNewScreenshotTexture2D(SentryUnityOptions options)
        => SentryScreenshot.CreateNewScreenshotTexture2D(options);

    internal virtual void CaptureAttachment(SentryId eventId, SentryAttachment attachment)
        => (Sentry.SentrySdk.CurrentHub as Hub)?.CaptureAttachment(eventId, attachment);

    internal virtual YieldInstruction WaitForEndOfFrame()
        => new WaitForEndOfFrame();
}
