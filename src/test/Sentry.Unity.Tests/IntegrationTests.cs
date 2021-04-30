using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.TestBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public sealed class IntegrationTests
    {
        [UnityTest]
        public IEnumerator BugFarmScene_ObjectCreatedWithExceptionLogicAndCalled_OneEventIsCreated()
        {
            yield return SetupSceneCoroutine("BugFarmScene");

            // arrange
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            // act
            /*
             * We don't want to call testBehaviour.TestException(); because it won't go via Sentry infra.
             * We don't have it in tests, but in scenes.
             */
            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(1, testEventCapture.Events.Count);
        }

        private static IEnumerator SetupSceneCoroutine(string sceneName)
        {
            // load scene with initialized Sentry, SceneManager.LoadSceneAsync(sceneName);
            SceneManager.LoadScene(sceneName);

            // skip a frame for a Unity to properly load a scene
            yield return null;

            // don't fail test if exception is thrown via 'SendMessage', we want to continue
            LogAssert.ignoreFailingMessages = true;
        }

        private static IDisposable InitSentrySdk(IEventCapture eventCapture)
        {
            SentryUnity.Init(options =>
            {
                options.Enabled = true;
                options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
                options.DiagnosticLogger = new UnityLogger(SentryLevel.Warning);
                options.AddIntegration(new UnityApplicationLoggingIntegration(null, eventCapture));
            });
            return new SentryDisposable();
        }

        private sealed class SentryDisposable : IDisposable
        {
            public void Dispose() => SentrySdk.Close();
        }
    }

    /*
     * Example of event capture which is used in Sentry.Unity infra
     */
    internal sealed class TestEventCapture : IEventCapture
    {
        private readonly List<SentryEvent> _events = new();

        public IReadOnlyCollection<SentryEvent> Events => _events.AsReadOnly();

        public SentryId Capture(SentryEvent sentryEvent)
        {
            _events.Add(sentryEvent);
            return sentryEvent.EventId;
        }
    }
}
