using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Sentry.Unity.Editor.Tests
{
    public class SentryScriptableObjectTests
    {
        private string _tempPath = null!; // Assigned in Setup

        [SetUp]
        public void Setup() => _tempPath = Path.Combine("Assets", Guid.NewGuid().ToString(), "TestOptions.asset");

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Path.GetDirectoryName(_tempPath));

        [Test]
        public void Load_ScriptableSentryUnityOptionsDoesNotExist_ReturnsNewObject()
        {
            SentryScriptableObject.Load<ScriptableSentryUnityOptions>(_tempPath);

            Assert.IsTrue(File.Exists(_tempPath));
        }

        [Test]
        public void Load_ScriptableSentryUnityOptionsAlreadyExist_ReturnsObject()
        {
            var expectedDsn = "test_dsn";
            var options = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(_tempPath);
            options.Dsn = expectedDsn;
            AssetDatabase.SaveAssets(); // Saving to disk

            var actualOptions = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(_tempPath);

            Assert.AreEqual(expectedDsn, actualOptions.Dsn);
        }

        [Test]
        public void Load_SentryCliOptionsDoesNotExist_ReturnsNewObject()
        {
            SentryScriptableObject.Load<SentryCliOptions>(_tempPath);

            Assert.IsTrue(File.Exists(_tempPath));
        }

        [Test]
        public void Load_SentryCliOptionsAlreadyExist_ReturnsObject()
        {
            var expectedAuth = "test_auth";
            var options = SentryScriptableObject.Load<SentryCliOptions>(_tempPath);
            options.Auth = expectedAuth;
            AssetDatabase.SaveAssets(); // Saving to disk

            var actualOptions = SentryScriptableObject.Load<SentryCliOptions>(_tempPath);

            Assert.AreEqual(expectedAuth, actualOptions.Auth);
        }
    }
}
