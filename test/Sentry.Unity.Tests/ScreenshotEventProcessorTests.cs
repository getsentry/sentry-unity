using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Extensibility;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    private class TestScreenshotCapturer : SentryScreenshotCapturer
    {
        public int RequestCount { get; private set; }
        public bool Available { get; set; } = true;
        public Func<Texture2D, Texture2D?>? Transform { get; private set; }
        public Func<SentryScreenshotPixels, bool>? BeforeEncode { get; private set; }

        public TestScreenshotCapturer(SentryUnityOptions options) : base(options, useAsyncReadback: false) { }

        internal override IAttachmentContent? TryRequest(Func<Texture2D, Texture2D?>? transform = null,
            Func<SentryScreenshotPixels, bool>? beforeEncode = null)
        {
            RequestCount++;
            Transform = transform;
            BeforeEncode = beforeEncode;
            return Available ? new ByteAttachmentContent(new byte[] { 1, 2, 3 }) : null;
        }
    }

    private static (ScreenshotEventProcessor Processor, TestScreenshotCapturer Capturer) GetSut(SentryUnityOptions? options = null)
    {
        options ??= new SentryUnityOptions();
        var capturer = new TestScreenshotCapturer(options);
        return (new ScreenshotEventProcessor(options, capturer), capturer);
    }

    [Test]
    public void Process_ScreenshotAvailable_AttachesToHint()
    {
        var (processor, _) = GetSut();
        var hint = new SentryHint();

        processor.Process(new SentryEvent(), hint);

        var attachment = hint.Attachments.Single();
        Assert.AreEqual("screenshot.jpg", attachment.FileName);
        Assert.AreEqual("image/jpeg", attachment.ContentType);
        Assert.AreEqual(AttachmentType.Default, attachment.Type);
    }

    [Test]
    public void Process_CapturerReturnsNothing_DoesNotAttach()
    {
        var (processor, capturer) = GetSut();
        capturer.Available = false;
        var hint = new SentryHint();

        processor.Process(new SentryEvent(), hint);

        Assert.IsEmpty(hint.Attachments);
    }

    [Test]
    public void Process_MultipleEventsInTheSameFrame_EachGetsAnAttachment()
    {
        var (processor, _) = GetSut();
        var firstHint = new SentryHint();
        var secondHint = new SentryHint();

        processor.Process(new SentryEvent(), firstHint);
        processor.Process(new SentryEvent(), secondHint);

        Assert.AreEqual(1, firstHint.Attachments.Count);
        Assert.AreEqual(1, secondHint.Attachments.Count);
    }

    [Test]
    public void Process_BeforeCaptureScreenshotReturnsFalse_SkipsTheCapturerEntirely()
    {
        var options = new SentryUnityOptions();
        options.SetBeforeCaptureScreenshot(_ => false);
        var (processor, capturer) = GetSut(options);
        var hint = new SentryHint();

        processor.Process(new SentryEvent(), hint);

        Assert.AreEqual(0, capturer.RequestCount);
        Assert.IsEmpty(hint.Attachments);
    }

    [Test]
    public void Process_OffTheMainThread_AttachesToHint()
    {
        // Errors raised on background threads must not lose their screenshot
        var (processor, capturer) = GetSut();
        var hint = new SentryHint();

        Task.Run(() => processor.Process(new SentryEvent(), hint)).Wait();

        Assert.AreEqual(1, capturer.RequestCount);
        Assert.AreEqual(1, hint.Attachments.Count);
    }

    [Test]
    public void Process_BeforeCaptureScreenshotThrows_ReturnsEventWithoutAttachment()
    {
        var options = new SentryUnityOptions();
        options.SetBeforeCaptureScreenshot(_ => throw new InvalidOperationException("test"));
        var (processor, capturer) = GetSut(options);
        var hint = new SentryHint();
        var @event = new SentryEvent();

        var processed = processor.Process(@event, hint);

        Assert.AreSame(@event, processed);
        Assert.AreEqual(0, capturer.RequestCount);
        Assert.IsEmpty(hint.Attachments);
    }

    [Test]
    public void Process_Feedback_DoesNotAttach()
    {
        var (processor, capturer) = GetSut();
        var feedback = new SentryEvent();
        feedback.Contexts.Feedback = new SentryFeedback("message");
        var hint = new SentryHint();

        processor.Process(feedback, hint);

        Assert.AreEqual(0, capturer.RequestCount);
        Assert.IsEmpty(hint.Attachments);
    }

    [Test]
    public void Process_BeforeCaptureScreenshotReceivesEvent()
    {
        var options = new SentryUnityOptions();
        SentryEvent? receivedEvent = null;
        options.SetBeforeCaptureScreenshot(@event =>
        {
            receivedEvent = @event;
            return true;
        });
        var (processor, _) = GetSut(options);

        var eventId = SentryId.Create();
        processor.Process(new SentryEvent(eventId: eventId), new SentryHint());

        Assert.NotNull(receivedEvent);
        Assert.AreEqual(eventId, receivedEvent!.EventId);
    }

    [Test]
    public void Process_NoBeforeSendScreenshot_RequestsWithoutTransform()
    {
        var (processor, capturer) = GetSut();

        processor.Process(new SentryEvent(), new SentryHint());

        Assert.IsNull(capturer.Transform);
    }

    [Test]
    public void Process_BeforeSendScreenshot_PassesTheScreenshotAndEventThroughTheTransform()
    {
        var options = new SentryUnityOptions();
        Texture2D? receivedScreenshot = null;
        SentryEvent? receivedEvent = null;
#pragma warning disable CS0618 // Still supported until removed
        options.SetBeforeSendScreenshot((screenshot, @event) =>
        {
            receivedScreenshot = screenshot;
            receivedEvent = @event;
            return screenshot;
        });
#pragma warning restore CS0618
        var (processor, capturer) = GetSut(options);

        var eventId = SentryId.Create();
        processor.Process(new SentryEvent(eventId: eventId), new SentryHint());

        // The capturer applies the transform once the screenshot is taken
        Assert.IsNotNull(capturer.Transform);
        var screenshot = new Texture2D(1, 1);
        Assert.AreSame(screenshot, capturer.Transform!(screenshot));
        Assert.AreSame(screenshot, receivedScreenshot);
        Assert.AreEqual(eventId, receivedEvent!.EventId);
        UnityEngine.Object.Destroy(screenshot);
    }

    [UnityTest]
    public IEnumerator Process_ThroughTheSdk_ScreenshotRidesAlongTheEventEnvelope()
    {
        // The screenshot must be an item on the event's own envelope. Sending it separately
        // orphans the attachment whenever the event is dropped on ingestion.
        var httpHandler = new TestHttpClientHandler("ScreenshotEnvelopeTest");
        var options = new SentryUnityOptions(application: new TestApplication())
        {
            Dsn = SentryTests.TestDsn,
            CreateHttpMessageHandler = () => httpHandler
        };
        options.AddEventProcessor(new ScreenshotEventProcessor(options, new TestScreenshotCapturer(options)));

        SentrySdk.Init(options);

        try
        {
            var capturedId = SentrySdk.CaptureMessage("test message");
            Assert.AreNotEqual(SentryId.Empty, capturedId);

            yield return null;

            var request = httpHandler.GetEvent("screenshot.jpg", TimeSpan.FromSeconds(2));
            Assert.IsNotEmpty(request);
            StringAssert.Contains("test message", request);
        }
        finally
        {
            SentrySdk.Close();
        }
    }

    [Test]
    public void Process_BeforeSendScreenshotPixelsSet_PassesItToTheCapturerWithTheEvent()
    {
        var options = new SentryUnityOptions();
        SentryEvent? receivedEvent = null;
        options.SetBeforeSendScreenshotPixels((_, @event) =>
        {
            receivedEvent = @event;
            return true;
        });
        var (processor, capturer) = GetSut(options);

        var eventId = SentryId.Create();
        processor.Process(new SentryEvent(eventId: eventId), new SentryHint());

        // Applied by the frame right before encoding, on the SDK's worker
        Assert.IsNotNull(capturer.BeforeEncode);
        Assert.IsNull(capturer.Transform);
        Assert.IsTrue(capturer.BeforeEncode!(default));
        Assert.AreEqual(eventId, receivedEvent!.EventId);
    }

    [Test]
    public void Process_BothScreenshotCallbacksSet_PrefersThePixelsCallback()
    {
        var options = new SentryUnityOptions();
#pragma warning disable CS0618 // Still supported until removed
        options.SetBeforeSendScreenshot((screenshot, _) => screenshot);
#pragma warning restore CS0618
        options.SetBeforeSendScreenshotPixels((_, _) => true);
        var (processor, capturer) = GetSut(options);

        processor.Process(new SentryEvent(), new SentryHint());

        Assert.IsNotNull(capturer.BeforeEncode);
        Assert.IsNull(capturer.Transform);
    }
}
