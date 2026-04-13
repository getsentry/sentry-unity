using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    /// <summary>
    /// Subclass that mocks screenshot capture and WaitForEndOfFrame but uses the REAL
    /// CaptureAttachment implementation (via Hub.CaptureAttachment), allowing us to verify
    /// that the attachment envelope actually reaches the HTTP transport.
    /// </summary>
    private class RealCaptureScreenshotEventProcessor : ScreenshotEventProcessor
    {
        public RealCaptureScreenshotEventProcessor(SentryUnityOptions options, ISentryMonoBehaviour sentryMonoBehaviour)
            : base(options, sentryMonoBehaviour) { }

        internal override Texture2D CreateNewScreenshotTexture2D(SentryUnityOptions options)
            => new Texture2D(1, 1);

        internal override YieldInstruction WaitForEndOfFrame()
            => new YieldInstruction();

        // CaptureAttachment is intentionally NOT overridden — the base implementation
        // calls Hub.CaptureAttachment which sends a standalone attachment envelope.
    }

    private class TestScreenshotEventProcessor : ScreenshotEventProcessor
    {
        public Func<SentryUnityOptions, Texture2D> CreateScreenshotFunc { get; set; }
        public Action<SentryId, SentryAttachment> CaptureAttachmentAction { get; set; }
        public Func<YieldInstruction> WaitForEndOfFrameFunc { get; set; }

        public TestScreenshotEventProcessor(SentryUnityOptions options, ISentryMonoBehaviour sentryMonoBehaviour)
            : base(options, sentryMonoBehaviour)
        {
            CreateScreenshotFunc = _ => new Texture2D(1, 1);
            CaptureAttachmentAction = (_, _) => { };
            WaitForEndOfFrameFunc = () => new YieldInstruction();
        }

        internal override Texture2D CreateNewScreenshotTexture2D(SentryUnityOptions options)
            => CreateScreenshotFunc.Invoke(options);

        internal override void CaptureAttachment(SentryId eventId, SentryAttachment attachment)
            => CaptureAttachmentAction(eventId, attachment);

        internal override YieldInstruction WaitForEndOfFrame()
            => WaitForEndOfFrameFunc.Invoke();
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
        screenshotProcessor.CreateScreenshotFunc = _ =>
        {
            screenshotCaptureCallCount++;
            return new Texture2D(1, 1);
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

    [UnityTest]
    public IEnumerator Process_ScreenshotCaptureThrowsException_HandlesGracefully()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var screenshotProcessor = new TestScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

        screenshotProcessor.CreateScreenshotFunc = _ => throw new Exception("Screenshot capture failed");

        var attachmentCaptureCallCount = 0;
        screenshotProcessor.CaptureAttachmentAction = (_, _) =>
        {
            attachmentCaptureCallCount++;
        };

        var sentryEvent = new SentryEvent();
        screenshotProcessor.Process(sentryEvent);

        // Wait for the coroutine to complete - need to wait for processing
        yield return null;
        yield return null;

        Assert.IsTrue(sentryMonoBehaviour.StartCoroutineCalled);
        Assert.AreEqual(0, attachmentCaptureCallCount);
    }

    [UnityTest]
    public IEnumerator Process_BeforeSendScreenshotCallback_ReceivesScreenshotAndEvent()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var options = new SentryUnityOptions();

        Texture2D? receivedScreenshot = null;
        SentryEvent? receivedEvent = null;

        options.SetBeforeSendScreenshot((screenshot, @event) =>
        {
            receivedScreenshot = screenshot;
            receivedEvent = @event;
            return screenshot;
        });

        var screenshotProcessor = new TestScreenshotEventProcessor(options, sentryMonoBehaviour);

        var eventId = SentryId.Create();
        var sentryEvent = new SentryEvent(eventId: eventId);

        screenshotProcessor.Process(sentryEvent);

        yield return null;
        yield return null;

        Assert.NotNull(receivedScreenshot);
        Assert.NotNull(receivedEvent);
        Assert.AreEqual(eventId, receivedEvent!.EventId);
    }

    [UnityTest]
    public IEnumerator Process_BeforeSendScreenshotCallback_ReturnsNull_SkipsAttachment()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var options = new SentryUnityOptions();

        options.SetBeforeSendScreenshot((_, _) => null);

        var screenshotProcessor = new TestScreenshotEventProcessor(options, sentryMonoBehaviour);

        var attachmentCaptureCallCount = 0;
        screenshotProcessor.CaptureAttachmentAction = (_, _) =>
        {
            attachmentCaptureCallCount++;
        };

        var sentryEvent = new SentryEvent();
        screenshotProcessor.Process(sentryEvent);

        yield return null;
        yield return null;

        Assert.AreEqual(0, attachmentCaptureCallCount);
    }

    [UnityTest]
    public IEnumerator Process_BeforeSendScreenshotCallbackReturnsNewTexture_AttachesNewTexture()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var options = new SentryUnityOptions();

        var newTexture = new Texture2D(10, 10);
        var newTextureBytes = newTexture.EncodeToJPG(options.ScreenshotCompression);
        var beforeSendInvoked = false;

        options.SetBeforeSendScreenshot((_, _) =>
        {
            beforeSendInvoked = true;
            return newTexture;
        });

        var screenshotProcessor = new TestScreenshotEventProcessor(options, sentryMonoBehaviour);

        var attachmentCaptured = false;
        byte[]? capturedBytes = null;

        screenshotProcessor.CaptureAttachmentAction = (_, attachment) =>
        {
            attachmentCaptured = true;
            if (attachment.Content is ByteAttachmentContent byteContent)
            {
                using var stream = byteContent.GetStream();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                capturedBytes = memoryStream.ToArray();
            }
        };

        screenshotProcessor.Process(new SentryEvent());

        yield return null;
        yield return null;

        Assert.IsTrue(beforeSendInvoked); // Sanity Check
        Assert.IsTrue(attachmentCaptured); // Sanity Check
        Assert.NotNull(capturedBytes);
        Assert.AreEqual(newTextureBytes.Length, capturedBytes!.Length);

        UnityEngine.Object.Destroy(newTexture);
    }

    [UnityTest]
    public IEnumerator Process_BeforeSendScreenshotCallbackModifiesTexture_UsesModifiedTexture()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var options = new SentryUnityOptions();

        var callbackInvoked = false;
        byte[]? modifiedTextureBytes = null;

        options.SetBeforeSendScreenshot((screenshot, @event) =>
        {
            callbackInvoked = true;

            // User modifies the texture in place

            var pixels = screenshot.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.red;
            }
            screenshot.SetPixels(pixels);
            screenshot.Apply();

            modifiedTextureBytes = screenshot.EncodeToJPG(options.ScreenshotCompression);

            return screenshot;
        });

        var screenshotProcessor = new TestScreenshotEventProcessor(options, sentryMonoBehaviour);

        var attachmentCaptured = false;
        byte[]? capturedBytes = null;

        screenshotProcessor.CaptureAttachmentAction = (_, attachment) =>
        {
            attachmentCaptured = true;
            if (attachment.Content is ByteAttachmentContent byteContent)
            {
                using var stream = byteContent.GetStream();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                capturedBytes = memoryStream.ToArray();
            }
        };

        screenshotProcessor.Process(new SentryEvent());

        yield return null;
        yield return null;

        Assert.IsTrue(callbackInvoked); // Sanity Check
        Assert.IsTrue(attachmentCaptured); // Sanity Check
        Assert.NotNull(modifiedTextureBytes);
        Assert.NotNull(capturedBytes);
        Assert.AreEqual(modifiedTextureBytes!.Length, capturedBytes!.Length);
    }

    [UnityTest]
    public IEnumerator Process_BeforeCaptureScreenshotCallback_ReturnsFalse_SkipsCapture()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var options = new SentryUnityOptions();

        options.SetBeforeCaptureScreenshot(_ => false);

        var screenshotProcessor = new TestScreenshotEventProcessor(options, sentryMonoBehaviour);

        var screenshotCaptureCallCount = 0;
        screenshotProcessor.CreateScreenshotFunc = _ =>
        {
            screenshotCaptureCallCount++;
            return new Texture2D(1, 1);
        };

        screenshotProcessor.Process(new SentryEvent());

        yield return null;
        yield return null;

        // BeforeCaptureScreenshot should prevent capture entirely
        Assert.AreEqual(0, screenshotCaptureCallCount);
    }

    [UnityTest]
    public IEnumerator Process_BeforeCaptureScreenshotCallbackReturnsTrue_CapturesScreenshot()
    {
        var sentryMonoBehaviour = GetTestMonoBehaviour();
        var options = new SentryUnityOptions();

        var callbackInvoked = false;
        options.SetBeforeCaptureScreenshot(_ =>
        {
            callbackInvoked = true;
            return true;
        });

        var screenshotProcessor = new TestScreenshotEventProcessor(options, sentryMonoBehaviour);

        var screenshotCaptureCallCount = 0;
        screenshotProcessor.CreateScreenshotFunc = _ =>
        {
            screenshotCaptureCallCount++;
            return new Texture2D(1, 1);
        };

        screenshotProcessor.Process(new SentryEvent());

        yield return null;
        yield return null;

        Assert.IsTrue(callbackInvoked);
        Assert.AreEqual(1, screenshotCaptureCallCount);
    }

    [UnityTest]
    public IEnumerator Process_EventDroppedByBeforeSend_ScreenshotAttachmentIsNotSent()
    {
        // When before_send drops an event (returns null), the screenshot should NOT be sent.
        // The screenshot processor's async coroutine must not send orphaned attachment envelopes
        // that count against quota but have no associated event in Sentry.

        var httpHandler = new TestHttpClientHandler("ScreenshotBeforeSendTest");
        var sdkOptions = new SentryUnityOptions(application: new TestApplication())
        {
            Dsn = SentryTests.TestDsn,
            CreateHttpMessageHandler = () => httpHandler
        };
        sdkOptions.SetBeforeSend((_, _) => null); // Drop all events
        SentrySdk.Init(sdkOptions);

        try
        {
            // Sanity check: verify before_send drops events
            var capturedEventId = SentrySdk.CaptureMessage("test message");
            Assert.AreEqual(SentryId.Empty, capturedEventId, "Sanity check: before_send should drop events");

            // Create a processor that mocks screenshot capture but uses the real CaptureAttachment
            // pathway (Hub.CaptureAttachment → Envelope.FromAttachment → HTTP transport)
            var sentryMonoBehaviour = GetTestMonoBehaviour();
            var processor = new RealCaptureScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);

            var sentryEvent = new SentryEvent();
            processor.Process(sentryEvent);

            // Wait for the coroutine to complete (fires at end of frame)
            yield return null;
            yield return null;

            // The screenshot attachment must not be sent when the event was dropped by before_send
            var screenshotRequest = httpHandler.GetEvent("screenshot.jpg", TimeSpan.FromSeconds(2));
            Assert.IsEmpty(screenshotRequest,
                "Screenshot attachment should not be sent when before_send drops the event");
        }
        finally
        {
            SentrySdk.Close();
        }
    }

    private static TestSentryMonoBehaviour GetTestMonoBehaviour()
    {
        var gameObject = new GameObject("ScreenshotProcessorTest");
        var behaviour = gameObject.AddComponent<TestSentryMonoBehaviour>();
        return behaviour;
    }
}
