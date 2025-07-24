using System;
using System.Collections;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    private class TestScreenshotEventProcessor : ScreenshotEventProcessor
    {
        public Func<SentryUnityOptions, byte[]> CaptureScreenshotFunc { get; set; }
        public Action<SentryId, SentryAttachment> CaptureAttachmentAction { get; set; }
        public Func<YieldInstruction> WaitForEndOfFrameFunc { get; set; }

        public TestScreenshotEventProcessor(SentryUnityOptions options, ISentryMonoBehaviour sentryMonoBehaviour)
            : base(options, sentryMonoBehaviour)
        {
            CaptureScreenshotFunc = _ => [0xFF, 0xD8, 0xFF];
            CaptureAttachmentAction = (_, _) => { };
            WaitForEndOfFrameFunc = () => new YieldInstruction();
        }

        internal override byte[] CaptureScreenshot(SentryUnityOptions options)
            => CaptureScreenshotFunc.Invoke(options);

        internal override void CaptureAttachment(SentryId eventId, SentryAttachment attachment)
            => CaptureAttachmentAction(eventId, attachment);

        internal override YieldInstruction WaitForEndOfFrame()
            => WaitForEndOfFrameFunc!.Invoke();
    }
    [Test]
    public void Process_FirstCallInAFrame_StartsCoroutine()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var screenshotProcessor = new TestScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

        screenshotProcessor.Process(new SentryEvent());

        Assert.IsTrue(sentryMonoBehaviour.StartCoroutineCalled);
    }

    [UnityTest]
    public IEnumerator Process_ExecutesCoroutine_CapturesScreenshotAndCapturesAttachment()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var screenshotProcessor = new TestScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

        var capturedEventId = SentryId.Empty;
        SentryAttachment? capturedAttachment = null;
        screenshotProcessor.CaptureAttachmentAction = (eventId, attachment) =>
        {
            capturedEventId = eventId;
            capturedAttachment = attachment;
        };

        var eventId = SentryId.Create();
        var sentryEvent = new SentryEvent(eventId: eventId);

        screenshotProcessor.Process(sentryEvent);

        // Wait for the coroutine to complete - need to wait for processing
        yield return null;
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
        var screenshotProcessor = new TestScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

        var screenshotCaptureCallCount = 0;
        screenshotProcessor.CaptureScreenshotFunc = _ =>
        {
            screenshotCaptureCallCount++;
            return [0];
        };

        var attachmentCaptureCallCount = 0;
        screenshotProcessor.CaptureAttachmentAction = (_, _) =>
        {
            attachmentCaptureCallCount++;
        };

        // Process multiple events quickly (before any coroutine can complete)
        screenshotProcessor.Process(new SentryEvent());
        screenshotProcessor.Process(new SentryEvent());
        screenshotProcessor.Process(new SentryEvent());

        // Wait for the coroutine to complete - need to wait for processing
        yield return null;
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
