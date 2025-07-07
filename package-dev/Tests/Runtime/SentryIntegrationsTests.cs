#if UNITY_2020_3_OR_NEWER
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity
{
    public class SentryIntegrationsTests
    {
#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
        [UnityTest]
        public IEnumerator Configure_TranceSampleRateOne_AddsSceneManagerTracingIntegration()
        {
            var options = new SentryUnityOptions
            {
                Dsn = "http://publickey@localhost:8000/12345",
                TracesSampleRate = 1.0f
            };

            SentryIntegrations.Configure(options);
            using var _ = InitSentrySdk(options);

            yield return null;

            Assert.IsNotNull(SceneManagerAPI.overrideAPI);
            Assert.AreEqual(typeof(SceneManagerTracingAPI), SceneManagerAPI.overrideAPI.GetType());
        }

        // TODO: To be fixed: Currently fails if run after the integration has successfully been added. (because it doesn't get removed)
        // [UnityTest]
        // public IEnumerator Configure_TranceSampleRateZero_DoesNotAddSceneManagerTracingIntegration()
        // {
        //     var options = new SentryUnityOptions
        //     {
        //         Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417",
        //         TracesSampleRate = 0f
        //     };
        //
        //     SentryIntegrations.Configure(options);
        //     using var _ = InitSentrySdk(options);
        //
        //     yield return null;
        //
        //     Assert.IsNull(SceneManagerAPI.overrideAPI);
        // }

        public static IDisposable InitSentrySdk(SentryUnityOptions options)
        {
            SentrySdk.Init(options);
            return new SentryDisposable();
        }

        private sealed class SentryDisposable : IDisposable
        {
            public void Dispose() => SentrySdk.Close();
        }
#endif
    }
}
