using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class SentryScreenshotTests
{
    [Test]
    [TestCase(ScreenshotQuality.High, 1920)]
    [TestCase(ScreenshotQuality.Medium, 1280)]
    [TestCase(ScreenshotQuality.Low, 854)]
    public void GetTargetResolution_ReturnsTargetMaxSize(ScreenshotQuality quality, int expectedValue)
    {
        var actualValue = SentryScreenshot.GetTargetResolution(quality);

        Assert.AreEqual(expectedValue, actualValue);
    }

    [Test]
    [TestCase(ScreenshotQuality.High, 1920)]
    [TestCase(ScreenshotQuality.Medium, 1280)]
    [TestCase(ScreenshotQuality.Low, 854)]
    public void CaptureScreenshot_QualitySet_ScreenshotDoesNotExceedDimensionLimit(ScreenshotQuality quality, int maximumAllowedDimension)
    {
        var options = new SentryUnityOptions { ScreenshotQuality = quality };

        var bytes = SentryScreenshot.Capture(options, 2000, 2000);
        var texture = new Texture2D(1, 1); // Size does not matter. Will be overwritten by loading
        texture.LoadImage(bytes);

        Assert.IsTrue(texture.width <= maximumAllowedDimension && texture.height <= maximumAllowedDimension);
    }

    [Test]
    public void CaptureScreenshot_QualitySetToFull_ScreenshotInFullSize()
    {
        var testScreenSize = 2000;
        var options = new SentryUnityOptions { ScreenshotQuality = ScreenshotQuality.Full };

        var bytes = SentryScreenshot.Capture(options, testScreenSize, testScreenSize);
        var texture = new Texture2D(1, 1); // Size does not matter. Will be overwritten by loading
        texture.LoadImage(bytes);

        Assert.IsTrue(texture.width == testScreenSize && texture.height == testScreenSize);
    }
}
