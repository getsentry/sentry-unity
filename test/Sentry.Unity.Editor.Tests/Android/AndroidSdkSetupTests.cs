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
        private class Fixture
        {
            public TestLogger Logger { get; set; }
            public string FakeProjectPath { get; set; }
            public string UnityProjectPath { get; set; }
            public string GradleProjectPath { get; set; }

            public Fixture()
            {
                Logger = new TestLogger();
                FakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                UnityProjectPath = Path.Combine(FakeProjectPath, "UnityProject");
                GradleProjectPath = Path.Combine(FakeProjectPath, "GradleProject");
            }

            public AndroidSdkSetup GetSut()
                => new(Logger, UnityProjectPath, GradleProjectPath);
        }

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            DebugSymbolUploadTests.SetupFakeProject(_fixture.FakeProjectPath);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_fixture.FakeProjectPath, true);

        private Fixture _fixture = null!; // created through SetUp

        [Test]
        public void AddAndroidSdk_AddsSdkToGradleProject()
        {
            var sut = _fixture.GetSut();

            sut.AddAndroidSdk();

            Assert.IsTrue(Directory.Exists(Path.Combine(_fixture.GradleProjectPath, "unityLibrary", "android-sdk-repository")));
        }

        [Test]
        public void AddAndroidSdk_SdkAlreadyExists_LogsAndSkips()
        {
            var sut = _fixture.GetSut();
            sut.AddAndroidSdk();

            sut.AddAndroidSdk();

            Assert.IsTrue(Directory.Exists(Path.Combine(_fixture.GradleProjectPath, "unityLibrary", "android-sdk-repository"))); // sanity check
            Assert.IsTrue(_fixture.Logger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("Android SDK already detected")));
        }

        [Test]
        public void RemoveAndroidSdk_SdkExists_RemovesSdkFromGradleProject()
        {
            var expectedSdkDirectoryPath = Path.Combine(_fixture.GradleProjectPath, "unityLibrary", "android-sdk-repository");
            var sut = _fixture.GetSut();
            sut.AddAndroidSdk();
            Assert.IsTrue(Directory.Exists(expectedSdkDirectoryPath)); // Sanity check

            sut.RemoveAndroidSdk();

            Assert.IsFalse(Directory.Exists(expectedSdkDirectoryPath));
        }

        [Test]
        public void RemoveAndroidSdk_SdkDoesNotExist_Skips()
        {
            var sut = _fixture.GetSut();

            sut.RemoveAndroidSdk();

            Assert.IsFalse(Directory.Exists(Path.Combine(_fixture.GradleProjectPath, "unityLibrary", "android-sdk-repository"))); // sanity check
        }
    }
}
