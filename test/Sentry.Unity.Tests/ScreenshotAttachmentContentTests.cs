using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sentry.Unity.Tests
{
    public class ScreenshotAttachmentTests : DisabledSelfInitializationTests
    {
        private class Fixture
        {
            public SentryUnityOptions Options = new() {AttachScreenshot = true};
            public bool IsMainThread = true;

            public ScreenshotAttachmentContent GetSut()
            {
                var gameObject = new GameObject("TestSentryMonoBehaviour");
                var sentryMonoBehaviour = gameObject.AddComponent<SentryMonoBehaviour>();
                sentryMonoBehaviour.IsMainThread = () => IsMainThread;

                return new ScreenshotAttachmentContent(Options, sentryMonoBehaviour);
            }
        }

        private Fixture _fixture = null!;

        [SetUp]
        public new void Setup() => _fixture = new Fixture();

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
            _fixture.IsMainThread = true;
            var sut = _fixture.GetSut();

            var stream = sut.GetStream();

            Assert.IsNotNull(stream);
        }

        [Test]
        public void GetStream_IsNonMainThread_ReturnsNullStream()
        {
            _fixture.IsMainThread = false;
            var sut = _fixture.GetSut();

            var stream = sut.GetStream();

            Assert.AreEqual(Stream.Null, stream);
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
            var texture = new Texture2D(1,1); // Size does not matter. Will be overwritten by loading
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
            var texture = new Texture2D(1,1); // Size does not matter. Will be overwritten by loading
            texture.LoadImage(bytes);

            Assert.IsTrue(texture.width == testScreenSize && texture.height == testScreenSize);
        }
    }
}
