using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Protocol;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.TestBehaviours;
using UnityEditor;
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
            yield return SetupSceneCoroutine("1_BugFarmScene");

            // arrange
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });
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
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationProductNameAtVersionAsRelease()
        {
            yield return SetupSceneCoroutine("1_BugFarmScene");

            // arrange
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(Application.productName + "@" + Application.version, testEventCapture.Events.First().Release);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesCustomRelease()
        {
            yield return SetupSceneCoroutine("1_BugFarmScene");

            // arrange
            var customRelease = "CustomRelease";
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.Release = customRelease;
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(customRelease, testEventCapture.Events.First().Release);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationVersionAsRelease_WhenProductNameWhitespace()
        {
            yield return SetupSceneCoroutine("1_BugFarmScene");

            // arrange
            PlayerSettings.productName = " ";
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(Application.version, testEventCapture.Events.First().Release);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationVersionAsRelease_WhenProductNameEmpty()
        {
            yield return SetupSceneCoroutine("1_BugFarmScene");

            // arrange
            PlayerSettings.productName = null;
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(Application.version, testEventCapture.Events.First().Release);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationInEditorOrProduction()
        {
            yield return SetupSceneCoroutine("1_BugFarmScene");

            // arrange
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            var actual = testEventCapture.Events.First();
            Assert.AreEqual(Application.isEditor
                ? "editor"
                : "production",
                actual.Environment);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_MultipleSentryInit_SendEventForTheLatest()
        {
            yield return SetupSceneCoroutine("1_BugFarmScene");

            var sourceEventCapture = new TestEventCapture();
            var sourceDsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
            SentryUnity.Init(options =>
            {
                options.Dsn = sourceDsn;
                options.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: sourceEventCapture));
            });

            var nextEventCapture = new TestEventCapture();
            var nextDsn = "https://a520c186ed684a8aa7d5d334bd7dab52@o447951.ingest.sentry.io/5801250";
            SentryUnity.Init(options =>
            {
                options.Dsn = nextDsn;
                options.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: nextEventCapture));
            });

            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();
            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            Assert.AreEqual(0, sourceEventCapture.Events.Count, sourceDsn);
            Assert.AreEqual(1, nextEventCapture.Events.Count, nextDsn);
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

        private static IDisposable InitSentrySdk(Action<SentryUnityOptions> configure)
        {
            SentryUnity.Init(options =>
            {
                options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
                configure.Invoke(options);
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
