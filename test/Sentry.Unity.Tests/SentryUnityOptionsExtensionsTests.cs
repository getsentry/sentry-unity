using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests
{
    public class SentryUnityOptionsExtensionsTests
    {
        private class Fixture
        {
            public TestApplication TestApplication { get; set; } = new();
            public bool Enabled { get; set; } = true;
            public string Dsn { get; set; } = "http://test.com";
            public bool CaptureInEditor { get; set; } = true;

            public SentryUnityOptions GetSut() => new()
            {
                Enabled = Enabled,
                Dsn = Dsn,
                CaptureInEditor = CaptureInEditor
            };
        }

        private Fixture _fixture = new();

        [SetUp]
        public void SetUp() => _fixture = new Fixture();

        [Test]
        public void Validate_OptionsIsNull_ReturnsFalse()
        {
            SentryUnityOptions? options = null;

            var isValid = options.IsValid();

            Assert.IsFalse(isValid);
        }

        [Test]
        public void Validate_OptionsDisabled_ReturnsFalse()
        {
            _fixture.Enabled = false;
            var options = _fixture.GetSut();

            var isValid = options.IsValid();

            Assert.IsFalse(isValid);
        }

        [Test]
        public void Validate_DsnEmpty_ReturnsFalse()
        {
            _fixture.Dsn = string.Empty;
            var options = _fixture.GetSut();

            var isValid = options.IsValid();

            Assert.IsFalse(isValid);
        }

        [Test]
        public void ShouldInitializeSdk_CorrectlyConfiguredForEditor_ReturnsTrue()
        {
            var options = _fixture.GetSut();

            var shouldInitialize = options.ShouldInitializeSdk(_fixture.TestApplication);

            Assert.IsTrue(shouldInitialize);
        }

        [Test]
        public void ShouldInitializeSdk_CorrectlyConfigured_ReturnsTrue()
        {
            _fixture.TestApplication = new TestApplication(false);
            var options = _fixture.GetSut();

            var shouldInitialize = options.ShouldInitializeSdk(_fixture.TestApplication);

            Assert.IsTrue(shouldInitialize);
        }

        [Test]
        public void ShouldInitializeSdk_NotCaptureInEditorAndApplicationIsEditor_ReturnsFalse()
        {
            _fixture.CaptureInEditor = false;
            var options = _fixture.GetSut();

            var shouldInitialize = options.ShouldInitializeSdk(_fixture.TestApplication);

            Assert.IsFalse(shouldInitialize);
        }
    }
}
