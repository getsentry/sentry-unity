#if UNITY_2020_3_OR_NEWER
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using System;
using System.Collections;
using NUnit.Framework;
using Sentry.Unity.Tests;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity
{
    public class SentrySceneTracingIntegrationTests
    {
#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
        private SentryUnityOptions _options;
        private TestHttpClientHandler _testHttpClientHandler = null!; // Set in Setup
        private readonly TimeSpan _eventReceiveTimeout = TimeSpan.FromSeconds(1);

        [SetUp]
        public void SetUp()
        {
            _testHttpClientHandler = new TestHttpClientHandler();
            _options = new SentryUnityOptions
            {
                Dsn = "http://publickey@localhost:8000/12345",
                TracesSampleRate = 1.0f,
                CreateHttpMessageHandler = () => _testHttpClientHandler
            };
        }

        [UnityTest]
        public IEnumerator SceneManagerTracingIntegration_DuringSceneLoad_CreatesTransaction()
        {
            SentryIntegrations.Configure(_options);
            using var _ = SentryIntegrationsTests.InitSentrySdk(_options);

            yield return SetupSceneCoroutine("1_Bugfarm");

            var triggeredEvent = _testHttpClientHandler.GetEvent("\"type\":\"transaction\"", _eventReceiveTimeout);

            Assert.That(triggeredEvent, Does.Contain(SceneManagerTracingAPI.TransactionOperation));
        }

        internal static IEnumerator SetupSceneCoroutine(string sceneName)
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene(sceneName);

            // skip a frame for a Unity to properly load a scene
            yield return null;
        }
#endif
    }
}
