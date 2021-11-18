using System;
using System.IO;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Editor.Tests
{
    public class SentryCliTests
    {
        [Test]
        public void GetSentryCliPlatformName_UnrecognizedPlatform_ThrowsInvalidOperationException()
        {
            var application = new TestApplication(platform: RuntimePlatform.CloudRendering);

            Assert.Throws<InvalidOperationException>(() => SentryCli.GetSentryCliPlatformName(application));
        }

        [Test]
        [TestCase(RuntimePlatform.WindowsEditor, "sentry-cli-Windows-x86_64.exe ")]
        [TestCase(RuntimePlatform.OSXEditor, "sentry-cli-Darwin-universal")]
        [TestCase(RuntimePlatform.LinuxEditor, "sentry-cli-Linux-x86_64 ")]
        public void GetSentryPlatformName_RecognizedPlatform_SetsSentryCliName(RuntimePlatform platform, string expectedName)
        {
            var application = new TestApplication(platform: platform);

            var actualName = SentryCli.GetSentryCliPlatformName(application);

            Assert.AreEqual(expectedName, actualName);
        }

        [Test]
        public void GetSentryCliPath_InvalidFileName_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => SentryCli.GetSentryCliPath("InvalidName"));
        }

        [Test]
        public void GetSentryCliPath_ValidFileName_ReturnsPath()
        {
           var sentryCliPlatformName = SentryCli.GetSentryCliPlatformName(new TestApplication(platform: Application.platform));
            var expectedPath = Path.GetFullPath(
                Path.Combine("Packages", SentryPackageInfo.GetName(), "Editor", "sentry-cli", sentryCliPlatformName));

            var actualPath = SentryCli.GetSentryCliPath(sentryCliPlatformName);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void SetExecutePermission_TODO()
        {
            // TODO
        }

        [Test]
        public void CreateSentryProperties_TODO()
        {
            // TODO
        }


    }
}
