using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public class SentryOptionsUtilityTests
    {
        [Test]
        public void SetDefaults_IsGlobalModeEnabled_IsTrue()
        {
            var options = new SentryUnityOptions();

            SentryOptionsUtility.SetDefaults(options, new TestApplication());

            Assert.IsTrue(options.IsGlobalModeEnabled);
        }

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
        [TestCase("\n")]
        [TestCase("\t")]
        [TestCase("/")]
        [TestCase("\\")]
        [TestCase("..")]
        [TestCase("@")]
        public void SetDefaults_Release_DoesNotContainInvalidCharacters(string invalidString)
        {
            var options = new SentryUnityOptions();
            var prefix = "test";
            var suffix = "application";
            var version = "0.1.0";
            var application = new TestApplication(productName: $"{prefix}{invalidString}{suffix}", version: version);

            SentryOptionsUtility.SetDefaults(options, application);

            Assert.AreEqual($"{prefix}_{suffix}@{version}", options.Release);
        }

        [Test]
        public void SetDefaults_Release_IgnoresDotOnlyProductNames()
        {
            var options = new SentryUnityOptions();
            var version = "0.1.0";
            var application = new TestApplication(productName: ".........", version: version);

            SentryOptionsUtility.SetDefaults(options, application);

            Assert.AreEqual($"{version}", options.Release);
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
        public void SetDefaults_Environment_IsProductionWhenBuilding()
        {
            var options = new SentryUnityOptions();
            var application = new TestApplication(isEditor: true);

            SentryOptionsUtility.SetDefaults(options, application, isBuilding: true);

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

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SetDefaults_OptionsCreated_AreEqual(bool isBuilding)
        {
            var expectedOptions = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(expectedOptions, isBuilding: isBuilding);

            var scriptableOptions = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
            SentryOptionsUtility.SetDefaults(scriptableOptions);

            // These are config window specific differences in default values we actually want
            scriptableOptions.Debug = false;
            scriptableOptions.DebugOnlyInEditor = false;
            scriptableOptions.DiagnosticLevel = SentryLevel.Debug;

            var actualOptions = ScriptableSentryUnityOptions.ToSentryUnityOptions(scriptableOptions, isBuilding);

            SentryUnityOptionsTests.AssertOptions(expectedOptions, actualOptions);
        }
    }
}
