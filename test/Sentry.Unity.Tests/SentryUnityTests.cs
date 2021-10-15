using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    [TestFixture]
    public class SentryUnityTests : IPrebuildSetup, IPostBuildCleanup
    {
        // If an options scriptable object exists Sentry SDK initializes itself on 'BeforeSceneLoad'.
        // We check in prebuild if those options exist and are enabled, disable them and restore them on Cleanup
        private ScriptableSentryUnityOptions? _optionsToRestore;

        public void Setup()
        {
            var options = AssetDatabase.LoadAssetAtPath(ScriptableSentryUnityOptions.GetConfigPath(ScriptableSentryUnityOptions.ConfigName),
                typeof(ScriptableSentryUnityOptions)) as ScriptableSentryUnityOptions;
            if (options?.Enabled != true)
            {
                return;
            }

            Debug.Log("Sentry Options found: Disabling for the duration of the test.");
            _optionsToRestore = options;
            _optionsToRestore.Enabled = false;
        }

        public void Cleanup()
        {
            if (_optionsToRestore != null)
            {
                _optionsToRestore.Enabled = true;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (SentrySdk.IsEnabled)
            {
                SentrySdk.Close();
            }
        }

        [Test]
        public void SentryUnity_OptionsValid_Initializes()
        {
            var options = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(options);
            options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";

            SentryUnity.Init(options);

            Assert.IsTrue(SentrySdk.IsEnabled);
        }

        [Test]
        public void SentryUnity_OptionsInvalid_DoesNotInitialize()
        {
            var options = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(options);

            // Even tho the defaults are set the DSN is missing making the options invalid for initialization
            SentryUnity.Init(options);

            Assert.IsFalse(SentrySdk.IsEnabled);
        }
    }
}
