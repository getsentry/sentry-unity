using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Sentry.Unity.Tests.TestBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public sealed class PlayModeTests
    {
        [UnityTest]
        public IEnumerator BugFarmScene_ObjectCreatedWithExceptionLogicAndCalled_OneEventIsCreated()
        {
            yield return SetupSceneCoroutine("BugFarmScene");

            // arrange
            var testEventCapture = CreateAndSetupSentryTestService();
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

        [UnityTest]
        public IEnumerator BugFarmScene_ThrowExceptionTwice_Outputs1Event()
        {
            yield return SetupSceneCoroutine("BugFarmScene");

            // arrange
            var testEventCapture = CreateAndSetupSentryTestService();
            /*
             * We should NOT use 'GameObject.Find', it's quite expensive.
             * 'GameObject.FindWithTag' is better but it needs additional setup.
             */
            var scriptsGameObject = GameObject.Find("Scripts");

            // act
            const string throwNullName = "ThrowNull";
            scriptsGameObject.SendMessage(throwNullName); // first exception
            scriptsGameObject.SendMessage(throwNullName); // second exception

            // assert
            Assert.AreEqual(1, testEventCapture.Events.Count);
        }

        [UnityTest]
        public IEnumerator EmptyScene_LogErrorAndException_Outputs2Events()
        {
            yield return SetupSceneCoroutine("EmptyScene");

            // arrange
            var testEventCapture = CreateAndSetupSentryTestService();

            // act
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();
            testBehaviour.SendMessage(nameof(testBehaviour.DebugLogError)); // Debug messages are in Breadcrumbs and not sent separately
            testBehaviour.SendMessage(nameof(testBehaviour.TestException));

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

        private static TestEventCapture CreateAndSetupSentryTestService()
        {
            var testEventCapture = new TestEventCapture();
            SentryInitialization.EventCapture = testEventCapture;
            return testEventCapture;
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
