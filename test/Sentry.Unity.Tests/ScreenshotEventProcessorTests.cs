using System.IO;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using UnityEngine;
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
        if (SentrySdk.IsEnabled)
        {
            SentryUnity.Close();
        }
    }

    [Test]
    [TestCase(ScreenshotQuality.High, 1920)]
    [TestCase(ScreenshotQuality.Medium, 1280)]
    [TestCase(ScreenshotQuality.Low, 854)]
    public void GetTargetResolution_ReturnsTargetMaxSize(ScreenshotQuality quality, int expectedValue)
    {
        var actualValue = ScreenshotEventProcessor.GetTargetResolution(quality);

        Assert.AreEqual(expectedValue, actualValue);
    }

    [Test]
    public void Process_IsMainThread_AddsScreenshotToHint()
    {
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
        _fixture.Options.SetBeforeCaptureScreenshot(() => captureScreenshot);
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.AreEqual(captureScreenshot ? 1 : 0, hint.Attachments.Count);
    }

    [Test]
    [TestCase(ScreenshotQuality.High, 1920)]
    [TestCase(ScreenshotQuality.Medium, 1280)]
    [TestCase(ScreenshotQuality.Low, 854)]
    public void CaptureScreenshot_QualitySet_ScreenshotDoesNotExceedDimensionLimit(ScreenshotQuality quality, int maximumAllowedDimension)
    {
        _fixture.Options.ScreenshotQuality = quality;
        var sut = _fixture.GetSut();

        var bytes = sut.CaptureScreenshot(2000, 2000);
        var texture = new Texture2D(1, 1); // Size does not matter. Will be overwritten by loading
        texture.LoadImage(bytes);

        Assert.IsTrue(texture.width <= maximumAllowedDimension && texture.height <= maximumAllowedDimension);
    }

    [Test]
    public void CaptureScreenshot_QualitySetToFull_ScreenshotInFullSize()
    {
        var testScreenSize = 2000;
        _fixture.Options.ScreenshotQuality = ScreenshotQuality.Full;
        var sut = _fixture.GetSut();

        var bytes = sut.CaptureScreenshot(testScreenSize, testScreenSize);
        var texture = new Texture2D(1, 1); // Size does not matter. Will be overwritten by loading
        texture.LoadImage(bytes);

        Assert.IsTrue(texture.width == testScreenSize && texture.height == testScreenSize);
    }

    [Test]
    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void Process_InEditorEnvironment_DoesNotCaptureScreenshot(bool isEditor, int expectedAttachmentCount)
    {
        // Arrange
        _fixture.TestApplication = new TestApplication(isEditor);
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        // Act
        sut.Process(sentryEvent, hint);

        // Assert
        Assert.AreEqual(expectedAttachmentCount, hint.Attachments.Count);
    }
}
