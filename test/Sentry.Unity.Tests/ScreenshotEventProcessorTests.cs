using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    private class TestScreenshotEventProcessor : ScreenshotEventProcessor
    {
        public Func<SentryUnityOptions, Texture2D> CreateScreenshotFunc { get; set; }
        public YieldInstruction? EndOfFrame { get; set; }

        public TestScreenshotEventProcessor(SentryUnityOptions options, ISentryMonoBehaviour sentryMonoBehaviour)
            : base(options, sentryMonoBehaviour)
        {
            CreateScreenshotFunc = _ => new Texture2D(1, 1);
        }

        internal override bool CanReadBackAsync() => false;

        internal override Texture2D CreateNewScreenshotTexture2D(SentryUnityOptions options)
            => CreateScreenshotFunc.Invoke(options);

        // WaitForEndOfFrame never resumes in batch mode
        internal override YieldInstruction? WaitForEndOfFrame() => EndOfFrame;
    }

    private static TestScreenshotEventProcessor GetSut(SentryUnityOptions? options = null)
    {
        var gameObject = new GameObject("ScreenshotProcessorTest");
        var behaviour = gameObject.AddComponent<TestSentryMonoBehaviour>();
        return new TestScreenshotEventProcessor(options ?? new SentryUnityOptions(), behaviour);
    }

    // The SDK reads attachments on its worker, where reading waits for the screenshot
    private static Task<byte[]> ReadOnWorker(SentryAttachment attachment) => Task.Run(() =>
    {
        using var stream = attachment.Content.GetStream();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    });

    [Test]
    public void Process_AttachesScreenshotToHint()
    {
        var hint = new SentryHint();

        GetSut().Process(new SentryEvent(), hint);

        var attachment = hint.Attachments.Single();
        Assert.AreEqual("screenshot.jpg", attachment.FileName);
        Assert.AreEqual("image/jpeg", attachment.ContentType);
        Assert.AreEqual(AttachmentType.Default, attachment.Type);
    }

    [UnityTest]
    public IEnumerator Process_EventsInTheSameFrame_ShareOneScreenshot()
    {
        var processor = GetSut();
        var captureCount = 0;
        processor.CreateScreenshotFunc = _ =>
        {
            captureCount++;
            return new Texture2D(1, 1);
        };
        var firstHint = new SentryHint();
        var secondHint = new SentryHint();

        processor.Process(new SentryEvent(), firstHint);
        processor.Process(new SentryEvent(), secondHint);

        var first = ReadOnWorker(firstHint.Attachments.Single());
        var second = ReadOnWorker(secondHint.Attachments.Single());
        yield return new WaitUntil(() => first.IsCompleted && second.IsCompleted);

        Assert.AreEqual(1, captureCount);
        Assert.IsNotEmpty(first.Result);
        CollectionAssert.AreEqual(first.Result, second.Result);
    }

    [Test]
    public void Process_BeforeCaptureScreenshotReturnsFalse_DoesNotAttach()
    {
        var options = new SentryUnityOptions();
        options.SetBeforeCaptureScreenshot(_ => false);
        var hint = new SentryHint();

        GetSut(options).Process(new SentryEvent(), hint);

        Assert.IsEmpty(hint.Attachments);
    }

    [Test]
    public void Process_BeforeCaptureScreenshotThrows_ReturnsEventWithoutAttachment()
    {
        var options = new SentryUnityOptions();
        options.SetBeforeCaptureScreenshot(_ => throw new InvalidOperationException("test"));
        var hint = new SentryHint();
        var @event = new SentryEvent();

        var processed = GetSut(options).Process(@event, hint);

        Assert.AreSame(@event, processed);
        Assert.IsEmpty(hint.Attachments);
    }

    [Test]
    public void Process_Feedback_DoesNotAttach()
    {
        var feedback = new SentryEvent();
        feedback.Contexts.Feedback = new SentryFeedback("message");
        var hint = new SentryHint();

        GetSut().Process(feedback, hint);

        Assert.IsEmpty(hint.Attachments);
    }

    [UnityTest]
    public IEnumerator Process_ScreenshotCaptureThrows_AttachmentIsEmpty()
    {
        var processor = GetSut();
        processor.CreateScreenshotFunc = _ => throw new Exception("Screenshot capture failed");
        var hint = new SentryHint();

        processor.Process(new SentryEvent(), hint);

        var read = ReadOnWorker(hint.Attachments.Single());
        yield return new WaitUntil(() => read.IsCompleted);

        Assert.IsEmpty(read.Result);
    }

    [UnityTest]
    public IEnumerator Process_BeforeSendScreenshot_ReceivesScreenshotAndEvent()
    {
        var options = new SentryUnityOptions();
        Texture2D? receivedScreenshot = null;
        SentryEvent? receivedEvent = null;
        options.SetBeforeSendScreenshot((screenshot, @event) =>
        {
            receivedScreenshot = screenshot;
            receivedEvent = @event;
            return screenshot;
        });
        var hint = new SentryHint();
        var eventId = SentryId.Create();

        GetSut(options).Process(new SentryEvent(eventId: eventId), hint);

        var read = ReadOnWorker(hint.Attachments.Single());
        yield return new WaitUntil(() => read.IsCompleted);

        Assert.NotNull(receivedScreenshot);
        Assert.AreEqual(eventId, receivedEvent!.EventId);
        Assert.IsNotEmpty(read.Result);
    }

    [UnityTest]
    public IEnumerator Process_BeforeSendScreenshotReturnsNull_AttachmentIsEmpty()
    {
        var options = new SentryUnityOptions();
        options.SetBeforeSendScreenshot((_, _) => null);
        var hint = new SentryHint();

        GetSut(options).Process(new SentryEvent(), hint);

        var read = ReadOnWorker(hint.Attachments.Single());
        yield return new WaitUntil(() => read.IsCompleted);

        Assert.IsEmpty(read.Result);
    }

    [UnityTest]
    public IEnumerator Process_BeforeSendScreenshotReturnsNewTexture_AttachesNewTexture()
    {
        var options = new SentryUnityOptions();
        var newTexture = new Texture2D(10, 10);
        var newTextureBytes = newTexture.EncodeToJPG(options.ScreenshotCompression);
        options.SetBeforeSendScreenshot((_, _) => newTexture);
        var hint = new SentryHint();

        GetSut(options).Process(new SentryEvent(), hint);

        var read = ReadOnWorker(hint.Attachments.Single());
        yield return new WaitUntil(() => read.IsCompleted);

        Assert.AreEqual(newTextureBytes.Length, read.Result.Length);
    }

    [UnityTest]
    public IEnumerator Process_PendingCaptureTimedOut_StartsNewCapture()
    {
        var processor = GetSut();
        // Keeps the first capture from completing, as if the main thread stopped rendering
        processor.EndOfFrame = new WaitForSeconds(60);
        var staleHint = new SentryHint();
        processor.Process(new SentryEvent(), staleHint);

        yield return new WaitForSecondsRealtime((float)SentryScreenshotAttachmentContent.CaptureTimeout.TotalSeconds + 0.1f);

        processor.EndOfFrame = null;
        var hint = new SentryHint();
        processor.Process(new SentryEvent(), hint);

        var stale = ReadOnWorker(staleHint.Attachments.Single());
        var read = ReadOnWorker(hint.Attachments.Single());
        yield return new WaitUntil(() => stale.IsCompleted && read.IsCompleted);

        Assert.IsEmpty(stale.Result);
        Assert.IsNotEmpty(read.Result);
    }

    [UnityTest]
    public IEnumerator GetStream_ReadAfterTheCaptureTimeout_DoesNotWaitAgain()
    {
        var content = new SentryScreenshotAttachmentContent(new TaskCompletionSource<byte[]?>().Task, new SentryUnityOptions());
        var attachment = new SentryAttachment(AttachmentType.Default, content, "screenshot.jpg", "image/jpeg");

        yield return new WaitForSecondsRealtime((float)SentryScreenshotAttachmentContent.CaptureTimeout.TotalSeconds + 0.1f);

        var stopwatch = Stopwatch.StartNew();
        var read = ReadOnWorker(attachment);
        yield return new WaitUntil(() => read.IsCompleted);

        Assert.Less(stopwatch.ElapsedMilliseconds, 500);
        Assert.IsEmpty(read.Result);
    }

    [UnityTest]
    public IEnumerator Process_ThroughTheSdk_ScreenshotRidesAlongTheEventEnvelope()
    {
        // Sending the screenshot separately orphans it whenever the event gets dropped on ingestion
        var httpHandler = new TestHttpClientHandler("ScreenshotEnvelopeTest");
        var options = new SentryUnityOptions(application: new TestApplication())
        {
            Dsn = SentryTests.TestDsn,
            CreateHttpMessageHandler = () => httpHandler
        };
        options.AddEventProcessor(GetSut(options));

        SentrySdk.Init(options);

        try
        {
            var capturedId = SentrySdk.CaptureMessage("test message");
            Assert.AreNotEqual(SentryId.Empty, capturedId);

            // Lets the screenshot get taken before blocking the main thread on the request
            yield return null;
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
}
