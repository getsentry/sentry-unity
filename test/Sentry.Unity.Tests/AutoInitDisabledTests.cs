using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    [TestFixture]
    public class AutoInitDisabledTests : IPrebuildSetup, IPostBuildCleanup
    {
        // If an options scriptable object exists Sentry SDK initializes itself on 'BeforeSceneLoad'.
        // We check in prebuild if those options exist and are enabled, disable them and restore them on Cleanup
        private ScriptableSentryUnityOptions? _optionsToRestore;

        public void Setup()
        {
            var options = AssetDatabase.LoadAssetAtPath<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());
            if (options == null || options.Enabled != true)
            {
                return;
            }

            Debug.Log($"Disabling local options for the duration of the '{GetType().Name}' tests.");
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
    }
}
