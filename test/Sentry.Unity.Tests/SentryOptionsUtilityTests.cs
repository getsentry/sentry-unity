using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests
{
    public class SentryOptionsUtilityTests
    {
        [Test]
        public void SetDefaults_Release_IsApplicationProductNameAtVersion()
        {
            var options = new SentryUnityOptions();
            var productName = "test-product";
            var version = "0.1.0";
            var application = new TestApplication(productName: productName, version: version);

            SentryOptionsUtility.SetDefaults(options, application);

            Assert.AreEqual($"{productName}@{version}", options.Release);
        }

        [Test]
        public void SetDefaults_Environment_IsEditorInEditor()
        {
            var options = new SentryUnityOptions();
            var application = new TestApplication(isEditor: true);

            SentryOptionsUtility.SetDefaults(options, application);

            Assert.AreEqual("editor", options.Environment);
        }

        [Test]
        public void SetDefaults_Environment_IsProductionOutsideEditor()
        {
            var options = new SentryUnityOptions();
            var application = new TestApplication(isEditor: false);

            SentryOptionsUtility.SetDefaults(options, application);

            Assert.AreEqual("production", options.Environment);
        }

        [Test]
        public void SetDefaults_CacheDirectoryPath_IsPersistentDataPath()
        {
            var options = new SentryUnityOptions();
            var persistentPath = "test/persistent/path";
            var application = new TestApplication(persistentDataPath: persistentPath);

            SentryOptionsUtility.SetDefaults(options, application);

            Assert.AreEqual(persistentPath, options.CacheDirectoryPath);
        }

        [Test]
        public void SetDefaults_IsEnvironmentUser_IsFalse()
        {
            var options = new SentryUnityOptions();

            SentryOptionsUtility.SetDefaults(options, new TestApplication());

            Assert.AreEqual(false, options.IsEnvironmentUser);
        }
    }
}
