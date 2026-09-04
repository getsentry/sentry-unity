using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    private class TestScreenshotCache : SentryScreenshotCache
    {
        public int EncodeCallCount { get; private set; }
        public byte[]? Bytes { get; set; } = { 1, 2, 3 };
        public Texture2D? TransformInput { get; set; }

        public TestScreenshotCache(SentryUnityOptions options) : base(options) { }

        internal override void Refresh() { }

        internal override byte[]? TryEncodeLatest(Func<Texture2D, Texture2D?>? transform = null)
        {
            EncodeCallCount++;

            if (transform is null)
            {
                return Bytes;
            }

            var input = TransformInput ??= new Texture2D(1, 1);
            return transform.Invoke(input) is null ? null : Bytes;
        }
    }

    private static (ScreenshotEventProcessor Processor, TestScreenshotCache Cache) GetSut(SentryUnityOptions? options = null)
    {
        options ??= new SentryUnityOptions();
        var cache = new TestScreenshotCache(options);
        return (new ScreenshotEventProcessor(options, cache), cache);
    }

    [Test]
    public void Process_CachedScreenshotAvailable_AttachesToHint()
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
    public void Process_CacheReturnsNothing_DoesNotAttach()
    {
        var (processor, cache) = GetSut();
        cache.Bytes = null;
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
    public void Process_BeforeCaptureScreenshotReturnsFalse_SkipsTheCacheEntirely()
    {
        var options = new SentryUnityOptions();
        options.SetBeforeCaptureScreenshot(_ => false);
        var (processor, cache) = GetSut(options);
        var hint = new SentryHint();

        processor.Process(new SentryEvent(), hint);

        Assert.AreEqual(0, cache.EncodeCallCount);
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
    public void Process_BeforeSendScreenshotReturnsNull_DoesNotAttach()
    {
        var options = new SentryUnityOptions();
        options.SetBeforeSendScreenshot((_, _) => null);
        var (processor, _) = GetSut(options);
        var hint = new SentryHint();

        processor.Process(new SentryEvent(), hint);

        Assert.IsEmpty(hint.Attachments);
    }

    [Test]
    public void Process_BeforeSendScreenshotReceivesScreenshotAndEvent()
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
        var (processor, cache) = GetSut(options);

        var eventId = SentryId.Create();
        processor.Process(new SentryEvent(eventId: eventId), new SentryHint());

        Assert.NotNull(receivedScreenshot);
        Assert.AreSame(cache.TransformInput, receivedScreenshot);
        Assert.NotNull(receivedEvent);
        Assert.AreEqual(eventId, receivedEvent!.EventId);
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
        options.AddEventProcessor(new ScreenshotEventProcessor(options, new TestScreenshotCache(options)));

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
}
