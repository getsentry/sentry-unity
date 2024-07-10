using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class ScreenshotAttachmentTests
{
    private class Fixture
    {
        public SentryUnityOptions Options = new() { AttachScreenshot = true };

        public ScreenshotAttachmentContent GetSut() => new(Options, SentryMonoBehaviour.Instance);
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
        var actualValue = ScreenshotAttachmentContent.GetTargetResolution(quality);

        Assert.AreEqual(expectedValue, actualValue);
    }

    [Test]
    public void GetStream_IsMainThread_ReturnsStream()
    {
        var sut = _fixture.GetSut();

        var stream = sut.GetStream();

        Assert.IsNotNull(stream);
    }

    [Test]
    public void GetStream_IsNonMainThread_ReturnsNullStream()
    {
        var sut = _fixture.GetSut();

        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            var stream = sut.GetStream();

            Assert.NotNull(stream);
            Assert.AreEqual(Stream.Null, stream);
        }).Start();
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
}