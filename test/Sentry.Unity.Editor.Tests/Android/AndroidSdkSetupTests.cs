using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sentry.Extensibility;
using Sentry.Unity.Editor.Android;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests.Android
{
    public class AndroidSdkSetupTests
    {
        private TestLogger Logger { get; set; } = new();
        private string FakeProjectPath { get; set; } = null!;
        private string UnityProjectPath { get; set; } = null!;
        private string GradleProjectPath { get; set; } = null!;

        [SetUp]
        public void SetUp()
        {
            FakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            UnityProjectPath = Path.Combine(FakeProjectPath, "UnityProject");
            GradleProjectPath = Path.Combine(FakeProjectPath, "GradleProject");

            DebugSymbolUploadTests.SetupFakeProject(FakeProjectPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(FakeProjectPath))
            {
                Directory.Delete(FakeProjectPath, true);
            }
        }

        [Test]
        public void AddAndroidSdk_AddsSdkToGradleProject()
        {
            var sut = new AndroidSdkSetup(Logger, UnityProjectPath, GradleProjectPath);

            sut.AddAndroidSdk();

            Assert.IsTrue(Directory.Exists(Path.Combine(GradleProjectPath, "unityLibrary", "android-sdk-repository")));
        }

        [Test]
        public void AddAndroidSdk_SdkAlreadyExists_LogsAndSkips()
        {
            var sut = new AndroidSdkSetup(Logger, UnityProjectPath, GradleProjectPath);
            sut.AddAndroidSdk();

            sut.AddAndroidSdk();

            Assert.IsTrue(Directory.Exists(Path.Combine(GradleProjectPath, "unityLibrary", "android-sdk-repository"))); // sanity check
            Assert.IsTrue(Logger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("Android SDK already detected")));
        }

        [Test]
        public void RemoveAndroidSdk_SdkExists_RemovesSdkFromGradleProject()
        {
            var expectedSdkDirectoryPath = Path.Combine(GradleProjectPath, "unityLibrary", "android-sdk-repository");
            var sut = new AndroidSdkSetup(Logger, UnityProjectPath, GradleProjectPath);
            sut.AddAndroidSdk();
            Assert.IsTrue(Directory.Exists(expectedSdkDirectoryPath)); // Sanity check

            sut.RemoveAndroidSdk();

            Assert.IsFalse(Directory.Exists(expectedSdkDirectoryPath));
        }

        [Test]
        public void RemoveAndroidSdk_SdkDoesNotExist_Skips()
        {
            var sut = new AndroidSdkSetup(Logger, UnityProjectPath, GradleProjectPath);

            sut.RemoveAndroidSdk();

            Assert.IsFalse(Directory.Exists(Path.Combine(GradleProjectPath, "unityLibrary", "android-sdk-repository"))); // sanity check
        }
    }
}
