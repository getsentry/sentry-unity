using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
            yield return SetupSceneCoroutine("1_BugFarm");

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
            yield return SetupSceneCoroutine("1_BugFarm");

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
            yield return SetupSceneCoroutine("1_BugFarm");

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
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var originalProductName = PlayerSettings.productName;
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

            PlayerSettings.productName = originalProductName;
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_UserNameIsEnvironmentUserNameWithDefaultPii()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
                o.SendDefaultPii = true;
                o.IsEnvironmentUser = true;
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(Environment.UserName, testEventCapture.Events.First().User.Username);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationVersionAsRelease_WhenProductNameEmpty()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var originalProductName = PlayerSettings.productName;
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

            PlayerSettings.productName = originalProductName;
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationInEditorOrProduction()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

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
        public IEnumerator BugFarmScene_EventCaptured_UserNameIsNotEnvironmentUserNameByDefault()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreNotEqual(Environment.UserName, testEventCapture.Events.First().User.Username);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_MultipleSentryInit_SendEventForTheLatest()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // We should use the sample Dsn for the nextDsn
            // to avoid static dsn.
            var options = AssetDatabase.LoadAssetAtPath(ScriptableSentryUnityOptions.GetConfigPath(ScriptableSentryUnityOptions.ConfigName),
                typeof(ScriptableSentryUnityOptions)) as ScriptableSentryUnityOptions;

            var sourceEventCapture = new TestEventCapture();
            var sourceDsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
           using var firstDisposable = InitSentrySdk(o =>
            {
                o.Dsn = sourceDsn;
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: sourceEventCapture));
            });

            var nextEventCapture = new TestEventCapture();
            var nextDsn = options?.Dsn;
            using var secondDisposable = InitSentrySdk(o =>
            {
                o.Dsn = nextDsn;
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: nextEventCapture));
            });
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();
            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            Assert.NotNull(nextDsn);
            Assert.AreNotEqual(sourceDsn, nextDsn);
            Assert.AreEqual(0, sourceEventCapture.Events.Count, sourceDsn);
            Assert.AreEqual(1, nextEventCapture.Events.Count);
        }

        [UnityTest]
        public IEnumerator Init_OptionsAreDefaulted()
        {
            yield return null;

            var expectedOptions = new SentryUnityOptions
            {
                Dsn = string.Empty // The SentrySDK tries to resolve the DSN from the environment when it's null
            };

            SentryUnityOptions? actualOptions = null;
            using var _ = InitSentrySdk(o =>
            {
                o.Dsn = string.Empty; // InitSentrySDK already sets a test dsn
                actualOptions = o;
            });

            Assert.NotNull(actualOptions);
            ScriptableSentryUnityOptionsTests.AssertOptions(expectedOptions, actualOptions!);
        }

        internal static IEnumerator SetupSceneCoroutine(string sceneName)
        {
            // load scene with initialized Sentry, SceneManager.LoadSceneAsync(sceneName);
            SceneManager.LoadScene(sceneName);

            // skip a frame for a Unity to properly load a scene
            yield return null;

            // don't fail test if exception is thrown via 'SendMessage', we want to continue
            LogAssert.ignoreFailingMessages = true;
        }

        internal static IDisposable InitSentrySdk(Action<SentryUnityOptions> configure)
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
