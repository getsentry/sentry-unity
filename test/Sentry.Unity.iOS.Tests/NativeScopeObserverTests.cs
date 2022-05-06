using System;
using System.Globalization;
using NUnit.Framework;

namespace Sentry.Unity.iOS.Tests
{
    public class IosNativeScopeObserverTests
    {
        [Test]
        public void GetTimestamp_ReturnStringConformsToISO8601()
        {
            var timestamp = DateTimeOffset.UtcNow;

            var timestampString = NativeScopeObserver.GetTimestamp(timestamp);
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
            var actualLevel = NativeScopeObserver.GetBreadcrumbLevel(level);

            Assert.AreEqual(actualLevel, expectedNativeLevel);
        }
    }
}
