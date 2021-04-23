using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var testEventCapture = new TestEventCapture();
            InitSentrySdk(opt => opt.EventCapture = testEventCapture);
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
            var testEventCapture = new TestEventCapture();
            InitSentrySdk(opt => opt.EventCapture = testEventCapture);

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
            var testEventCapture = new TestEventCapture();
            InitSentrySdk(opt => opt.EventCapture = testEventCapture);

            // act
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();
            testBehaviour.SendMessage(nameof(testBehaviour.DebugLogError)); // Debug messages are in Breadcrumbs and not sent separately
            testBehaviour.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(1, testEventCapture.Events.Count);
        }

        [UnityTest]
        public IEnumerator UnityEventExceptionProcessor_ILL2CPPStackTraceFilenameWithZeroes_ShouldReturnEmptyString()
        {
            yield return SetupSceneCoroutine("BugFarmScene");

            // arrange
            var unityEventProcessor = new UnityEventExceptionProcessor();
            var ill2CppUnityLogException = new UnityLogException(
                "one: two",
                "BugFarm.ThrowNull () (at <00000000000000000000000000000000>:0)");
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(ill2CppUnityLogException, sentryEvent);

            // assert
            Assert.NotNull(sentryEvent.SentryExceptions);

            var sentryException = sentryEvent.SentryExceptions!.First();
            Assert.NotNull(sentryException.Stacktrace);
            Assert.Greater(sentryException.Stacktrace!.Frames.Count, 0);

            var sentryExceptionFirstFrame = sentryException.Stacktrace!.Frames[0];
            Assert.AreEqual(string.Empty, sentryExceptionFirstFrame.FileName);
        }

        [UnityTest]
        public IEnumerator UnityEventProcessor_SdkInfo_Correct()
        {
            yield return SetupSceneCoroutine("BugFarmScene");

            // arrange
            var unityEventProcessor = new UnityEventProcessor();
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.AreEqual(UnitySdkInfo.Name, sentryEvent.Sdk.Name);
            Assert.AreEqual(UnitySdkInfo.Version, sentryEvent.Sdk.Version);

            var package = sentryEvent.Sdk.Packages.FirstOrDefault();
            Assert.IsNotNull(package);
            Assert.AreEqual(UnitySdkInfo.PackageName, package!.Name);
            Assert.AreEqual(UnitySdkInfo.Version, package!.Version);
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

        private static void InitSentrySdk(Action<SentryUnity>? sentryUnity = null)
            => SentryUnity.Init(
                opt =>
                {
                    opt.Enabled = true;
                    opt.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
                    opt.Logger = new UnityLogger(SentryLevel.Warning);
                },
                sentryUnity);
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
