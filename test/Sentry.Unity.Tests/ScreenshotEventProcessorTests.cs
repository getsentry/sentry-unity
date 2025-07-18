using System.Collections;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    [Test]
    public void Process_FirstCallInAFrame_StartsCoroutine()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var screenshotProcessor = new ScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

        screenshotProcessor.Process(new SentryEvent());

        Assert.IsTrue(sentryMonoBehaviour.StartCoroutineCalled);
    }

    [UnityTest]
    public IEnumerator Process_ExecutesCoroutine_CapturesScreenshotAndCapturesAttachment()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var screenshotProcessor = new ScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

        var capturedEventId = SentryId.Empty;
        SentryAttachment? capturedAttachment = null;
        screenshotProcessor.AttachmentCaptureFunction = (eventId, attachment) =>
        {
            capturedEventId = eventId;
            capturedAttachment = attachment;
        };

        var eventId = SentryId.Create();
        var sentryEvent = new SentryEvent(eventId: eventId);

        screenshotProcessor.Process(sentryEvent);

        // Wait for the coroutine to complete
        yield return null;

        Assert.IsTrue(sentryMonoBehaviour.StartCoroutineCalled);
        Assert.AreEqual(eventId, capturedEventId);
        Assert.NotNull(capturedAttachment); // Sanity check
        Assert.AreEqual("screenshot.jpg", capturedAttachment!.FileName);
        Assert.AreEqual("image/jpeg", capturedAttachment.ContentType);
        Assert.AreEqual(AttachmentType.Default, capturedAttachment.Type);
    }

    [UnityTest]
    public IEnumerator Process_CalledMultipleTimesQuickly_OnlyExecutesScreenshotCaptureOnce()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var screenshotProcessor = new ScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

        var screenshotCaptureCallCount = 0;
        screenshotProcessor.ScreenshotCaptureFunction = _ =>
        {
            screenshotCaptureCallCount++;
            return [0];
        };

        var attachmentCaptureCallCount = 0;
        screenshotProcessor.AttachmentCaptureFunction = (_, _) =>
        {
            attachmentCaptureCallCount++;
        };

        // Process multiple events quickly (before any coroutine can complete)
        screenshotProcessor.Process(new SentryEvent());
        screenshotProcessor.Process(new SentryEvent());
        screenshotProcessor.Process(new SentryEvent());

        // Wait for the coroutine to complete
        yield return null;

        Assert.AreEqual(1, screenshotCaptureCallCount);
        Assert.AreEqual(1, attachmentCaptureCallCount);
    }

    private static TestSentryMonoBehaviour GetTestMonoBehaviour()
    {
        var gameObject = new GameObject("ScreenshotProcessorTest");
        var behaviour = gameObject.AddComponent<TestSentryMonoBehaviour>();
        return behaviour;
    }
}
