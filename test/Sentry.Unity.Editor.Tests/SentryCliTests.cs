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
        public void SetExecutePermission_FileDoesNotExist_ThrowsUnauthorizedAccessException()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Assert.Inconclusive("Skipping chmod on Windows.");
                return;
            }

            Assert.Throws<UnauthorizedAccessException>(() => SentryCli.SetExecutePermission("non-existent-file"));
        }

        [Test]
        public void CreateSentryProperties_PropertyFileCreatedAndContainsSentryCliOptions()
        {
            var propertiesDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(propertiesDirectory);

            var sentryCliTestOptions = new SentryCliOptions();
            sentryCliTestOptions.Auth = Guid.NewGuid().ToString();
            sentryCliTestOptions.Organization = Guid.NewGuid().ToString();
            sentryCliTestOptions.Project = Guid.NewGuid().ToString();

            SentryCli.CreateSentryProperties(propertiesDirectory, sentryCliTestOptions);

            var properties = File.ReadAllText(Path.Combine(propertiesDirectory, "sentry.properties"));

            StringAssert.Contains(sentryCliTestOptions.Auth, properties);
            StringAssert.Contains(sentryCliTestOptions.Organization, properties);
            StringAssert.Contains(sentryCliTestOptions.Project, properties);

            Directory.Delete(propertiesDirectory, true);
        }
    }
}
