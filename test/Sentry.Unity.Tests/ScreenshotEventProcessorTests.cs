using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    private class Fixture
    {
        public SentryUnityOptions Options = new() { AttachScreenshot = true };
        public TestApplication TestApplication = new();

        public ScreenshotEventProcessor GetSut() => new(Options, TestApplication);
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [TearDown]
    public void TearDown()
    {
        if (Sentry.SentrySdk.IsEnabled)
        {
            SentrySdk.Close();
        }
    }

    [Test]
    public void Process_IsMainThread_AddsScreenshotToHint()
    {
        _fixture.TestApplication.IsEditor = false;
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.AreEqual(1, hint.Attachments.Count);
    }

    [Test]
    public void Process_IsNonMainThread_DoesNotAddScreenshotToHint()
    {
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            var stream = sut.Process(sentryEvent, hint);

            Assert.AreEqual(0, hint.Attachments.Count);
        }).Start();
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Process_BeforeCaptureScreenshotCallbackProvided_RespectsScreenshotCaptureDecision(bool captureScreenshot)
    {
        _fixture.TestApplication.IsEditor = false;
        _fixture.Options.SetBeforeCaptureScreenshot(() => captureScreenshot);
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.AreEqual(captureScreenshot ? 1 : 0, hint.Attachments.Count);
    }

    [Test]
    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void Process_InEditorEnvironment_DoesNotCaptureScreenshot(bool isEditor, int expectedAttachmentCount)
    {
        // Arrange
        _fixture.TestApplication.IsEditor = isEditor;
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        // Act
        sut.Process(sentryEvent, hint);

        // Assert
        Assert.AreEqual(expectedAttachmentCount, hint.Attachments.Count);
    }
}
