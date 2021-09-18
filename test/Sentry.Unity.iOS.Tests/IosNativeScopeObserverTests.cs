using System;
using System.Globalization;
using NUnit.Framework;
using Sentry.Infrastructure;

namespace Sentry.Unity.iOS.Tests
{
    public class IosNativeScopeObserverTests
    {
        private class SerializationTestClass
        {
            public string Member => throw new NullReferenceException();
        }

        private class Fixture
        {
            public SentryUnityOptions Options { get; set; } = new();
            public IosNativeScopeObserver GetSut() => new(Options);
        }

        private Fixture _fixture = new();

        [SetUp]
        public void SetUp() => _fixture = new Fixture();

        [Test]
        public void GetTimestamp_ReturnStringConformsToISO8601()
        {
            var timestamp = SystemClock.Clock.GetUtcNow();

            var timestampString = IosNativeScopeObserver.GetTimestamp(timestamp);
            var actualTimestamp = DateTimeOffset.ParseExact(timestampString, "o", CultureInfo.InvariantCulture);

            Assert.AreEqual(timestamp, actualTimestamp);
        }

        [Test]
        [TestCase(BreadcrumbLevel.Debug, 1)]
        [TestCase(BreadcrumbLevel.Info, 2)]
        [TestCase(BreadcrumbLevel.Warning, 3)]
        [TestCase(BreadcrumbLevel.Error, 4)]
        [TestCase(BreadcrumbLevel.Critical, 5)]
        public void GetBreadcrumbLevel_TestCases(BreadcrumbLevel level, int expectedNativeLevel)
        {
            var actualLevel = IosNativeScopeObserver.GetBreadcrumbLevel(level);

            Assert.AreEqual(actualLevel, expectedNativeLevel);
        }

        [Test]
        public void SerializeExtraValue_ValueSerializable_ReturnsSerializedValue()
        {
            var sut = _fixture.GetSut();

            var actualValue = sut.SerializeExtraValue(new { Member = "testString" });

            Assert.NotNull(actualValue);
            Assert.IsNotEmpty(actualValue);
        }

        [Test]
        public void SerializeExtraValue_ValueNotSerializable_ReturnsNull()
        {
            var sut = _fixture.GetSut();

            var actualValue = sut.SerializeExtraValue(new SerializationTestClass());

            Assert.AreEqual(null, actualValue);
        }
    }
}
