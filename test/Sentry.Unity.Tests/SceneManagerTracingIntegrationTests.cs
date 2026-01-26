using System;
using System.Collections;
using NUnit.Framework;
using Sentry.Unity.Tests;
using Sentry.Unity.Tests.Stubs;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity
{
    public class SentrySceneTracingIntegrationTests
    {
        private SentryUnityOptions _options = null!; // Set in Setup
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

        [TearDown]
        public void TearDown() => SceneManagerAPI.overrideAPI = null;

        [UnityTest]
        public IEnumerator SceneManagerTracingIntegration_DuringSceneLoad_CreatesTransaction()
        {
            // Arrange
            SentryUnitySdk.Init(_options);

            // Act
            yield return SetupSceneCoroutine("1_Bugfarm");

            // Assert
            var triggeredEvent = _testHttpClientHandler.GetEvent("\"type\":\"transaction\"", _eventReceiveTimeout);
            Assert.That(triggeredEvent, Does.Contain(SceneManagerTracingAPI.TransactionOperation));
        }

        [Test]
        public void SceneManagerTracingIntegration_SampleRateSetToZero_SkipsAddingIntegration()
        {
            // Arrange
            var sceneManagerTracingIntegration = new SceneManagerTracingIntegration();
            _options.TracesSampleRate = 0.0f;

            // Act
            sceneManagerTracingIntegration.Register(new TestHub(), _options);

            // Assert
            Assert.IsNull(SceneManagerAPI.overrideAPI);
        }

        [TestCase(1.0f, true, true)]
        [TestCase(1.0f, false, false)]
        [TestCase(0.0f, true, false)]
        [TestCase(0.0f, false, false)]
        [Test]
        public void SceneManagerTracingIntegration_RegistersBasedOnConfiguration(
            float tracesSampleRate, bool autoSceneLoadTraces, bool shouldRegister)
        {
            // Arrange
            var sceneManagerTracingIntegration = new SceneManagerTracingIntegration();
            _options.TracesSampleRate = tracesSampleRate;
            _options.AutoSceneLoadTraces = autoSceneLoadTraces;

            // Act
            sceneManagerTracingIntegration.Register(new TestHub(), _options);

            // Assert
            if (shouldRegister)
            {
                Assert.IsNotNull(SceneManagerAPI.overrideAPI);
                Assert.IsInstanceOf<SceneManagerTracingAPI>(SceneManagerAPI.overrideAPI);
            }
            else
            {
                Assert.IsNull(SceneManagerAPI.overrideAPI);
            }
        }

        internal static IEnumerator SetupSceneCoroutine(string sceneName)
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene(sceneName);

            // skip a frame for a Unity to properly load a scene
            yield return null;
        }
    }
}
