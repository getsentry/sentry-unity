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
        // Disabled (json approach)
        // [UnitySetUp]
        public IEnumerator InitializeOptions()
        {
            // Due to an issue, Sentry doesn't always load UnitySentryOptions, which
            // results in tests not running on clean clone or on CI.
            // https://github.com/getsentry/sentry-unity/issues/77
            //
            // This hack sets the options manually if that happens.
            // Since this skips a layer of testing, this is not desirable long term
            // and we should find a proper way to solve this.
            if (!SentryInitialization.IsInit)
            {
                var options = ScriptableObject.CreateInstance<UnitySentryOptions>();
                options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
                options.Enabled = true;

                SentryInitialization.Init(options);

                Debug.LogWarning("Sentry has not been initialized prior to running tests. Using manual configuration.");
            }

            yield break;
        }

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

        /*
         * TODO:
         *
         * The current Sentry initialization is static. It means that the initialization is done once for all the tests.
         * That's why we need to alter some state on 'per test' level before running them in bulk.
         *
         * This problem will be mitigated as we implement this https://github.com/getsentry/sentry-unity/issues/66
         */
        private static TestEventCapture CreateAndSetupSentryTestService()
        {
            var testEventCapture = new TestEventCapture();
            SentryInitialization.EventCapture = testEventCapture;
            SentryInitialization.ErrorTimeDebounce = new(TimeSpan.FromSeconds(1));
            SentryInitialization.LogTimeDebounce = new(TimeSpan.FromSeconds(1));
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
