#if UNITY_2020_3_OR_NEWER
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity
{
    [TestFixture]
    public class SentryIntegrationsTests : IPrebuildSetup, IPostBuildCleanup
    {
#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
        // If an options scriptable object exists Sentry SDK initializes itself on 'BeforeSceneLoad'.
        // We check in prebuild if those options exist and are enabled, disable them and restore them on Cleanup
        private ScriptableSentryUnityOptions _optionsToRestore;

        public void Setup()
        {
            var options = AssetDatabase.LoadAssetAtPath("Assets/Resources/Sentry/SentryOptions.asset",
                typeof(ScriptableSentryUnityOptions)) as ScriptableSentryUnityOptions;
            if (options?.Enabled != true)
            {
                return;
            }

            Debug.Log("Disabling local options for the duration of the test.");
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

        [UnityTest]
        public IEnumerator SceneManagerTracingIntegration_TranceSampleRateGreaterZero_AddsIntegration()
        {
            yield return null;

            using var _ = InitSentrySdk(options =>
            {
                options.TracesSampleRate = 1.0f;
            });

            Assert.IsNotNull(SceneManagerAPI.overrideAPI);
            Assert.AreEqual(typeof(SceneManagerTracingAPI), SceneManagerAPI.overrideAPI.GetType());
        }

        internal IDisposable InitSentrySdk(Action<SentryUnityOptions> configure = null)
        {
            SentryInitialization.Init();
            SentryUnity.Init(options =>
            {
                options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
                configure?.Invoke(options);
            });

            return new SentryDisposable();
        }

        private sealed class SentryDisposable : IDisposable
        {
            public void Dispose() => SentrySdk.Close();
        }
#endif
    }
}
