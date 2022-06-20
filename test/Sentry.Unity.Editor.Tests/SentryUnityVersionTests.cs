using System;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Editor.Tests
{
    public class SentryUnityVersionTests
    {
        [Test]
        [TestCase("2019.4.39f1", "2019.4.39")]
        [TestCase("2021.1.1b1", "2021.1.1")]
        [TestCase("2022.1.0a17", "2022.1.0")]
        [TestCase("2022.1.0", "2022.1.0")]
        public void GetUnityVersion_WellFormedVersion_ReturnsTrimmedVersion(string unityVersion, string expectedUnityVersion)
        {
            var application = new TestApplication(unityVersion: unityVersion);
            var expectedVersion = new Version(expectedUnityVersion);

            var actualVersion = SentryUnityVersion.GetVersion(application);

            Assert.AreEqual(expectedVersion, actualVersion);
        }

        [Test]
        [TestCase("asdf.asdf")]
        [TestCase("2019.4.1f1a3")]
        public void GetUnityVersion_MalformedVersion_Throws(string unityVersion)
        {
            var application = new TestApplication(unityVersion: unityVersion);

            Assert.Throws<FormatException>(() => SentryUnityVersion.GetVersion(application));
        }
    }
}
