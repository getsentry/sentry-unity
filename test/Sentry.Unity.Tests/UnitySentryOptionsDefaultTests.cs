using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public class UnitySentryOptionsDefaultTests
    {
        [Test]
        public void DefaultRelease_IsApplicationProductNameAtVersion()
        {
            // Arrange
            var options = new SentryUnityOptions();
            var application = new TestApplication(productName: Application.productName, version: Application.version);

            // Act
            SentryDefaultOptionSetter.SetRelease(options, application);

            // Assert
            Assert.AreEqual($"{Application.productName}@{Application.version}", options.Release);
        }

        [Test]
        public void DefaultRelease_DoesNotOverrideCustomRelease()
        {
            // Arrange
            var testRelease = "TestRelease";
            var options = new SentryUnityOptions {Release = testRelease};

            // Act
            SentryDefaultOptionSetter.SetRelease(options, new TestApplication());

            // Assert
            Assert.AreEqual(testRelease, options.Release);
        }

        [Test]
        public void DefaultEnvironment_IsEditorInEditor()
        {
            // Arrange
            var options = new SentryUnityOptions();
            var application = new TestApplication(isEditor: true);

            // Act
            SentryDefaultOptionSetter.SetEnvironment(options, application);

            // Assert
            Assert.AreEqual("editor", options.Environment);
        }

        [Test]
        public void DefaultEnvironment_IsProductionOutsideEditor()
        {
            // Arrange
            var options = new SentryUnityOptions();
            var application = new TestApplication(isEditor: false);

            // Act
            SentryDefaultOptionSetter.SetEnvironment(options, application);

            // Assert
            Assert.AreEqual("production", options.Environment);
        }

        [Test]
        public void DefaultEnvironment_DoesNotOverrideCustomEnvironment()
        {
            // Arrange
            var testEnvironment = "TestEnvironment";
            var options = new SentryUnityOptions {Environment = testEnvironment};

            // Act
            SentryDefaultOptionSetter.SetEnvironment(options, new TestApplication());

            // Assert
            Assert.AreEqual(testEnvironment, options.Environment);
        }

        [Test]
        public void DefaultCacheDirectoryPath_IsPersistentDataPath()
        {
            // Arrange
            var options = new SentryUnityOptions();
            var application = new TestApplication(persistentDataPath: Application.persistentDataPath);

            // Act
            SentryDefaultOptionSetter.SetCacheDirectoryPath(options, application);

            // Assert
            Assert.AreEqual(Application.persistentDataPath, options.CacheDirectoryPath);
        }

        [Test]
        public void DefaultCacheDirectoryPath_DoesNotOverrideCustomCacheDirectoryPath()
        {
            // Arrange
            var customCacheDirectoryPath = "custom/cache/directory/path";
            var options = new SentryUnityOptions { CacheDirectoryPath = customCacheDirectoryPath };

            // Act
            SentryDefaultOptionSetter.SetCacheDirectoryPath(options, new TestApplication());

            // Assert
            Assert.AreEqual(customCacheDirectoryPath, options.CacheDirectoryPath);
        }
    }
}
