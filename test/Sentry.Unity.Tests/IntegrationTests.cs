using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            // act
            /*
             * We don't want to call testBehaviour.TestException(); because it won't go via Sentry infra.
             * We don't have it in tests, but in scenes.
             */
            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(1, testEventCapture.Count);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationProductNameAtVersionAsRelease()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(Application.productName + "@" + Application.version, testEventCapture.First.Release);
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
            Assert.AreEqual(customRelease, testEventCapture.First.Release);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationVersionAsRelease_WhenProductNameWhitespace()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var originalProductName = PlayerSettings.productName;
            PlayerSettings.productName = " ";
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(Application.version, testEventCapture.First.Release);

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
            Assert.AreEqual(Environment.UserName, testEventCapture.First.User.Username);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationVersionAsRelease_WhenProductNameEmpty()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var originalProductName = PlayerSettings.productName;
            PlayerSettings.productName = null;
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreEqual(Application.version, testEventCapture.First.Release);

            PlayerSettings.productName = originalProductName;
        }

        [UnityTest]
        public IEnumerator BugFarmScene_EventCaptured_IncludesApplicationInEditorOrProduction()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            var actual = testEventCapture.First;
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
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            // assert
            Assert.AreNotEqual(Environment.UserName, testEventCapture.First.User.Username);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_MultipleSentryInit_SendEventForTheLatest()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            var sourceEventCapture = new TestEventCapture();
            var sourceDsn = "http://publickey@localhost:8000/12345";
            using var firstDisposable = InitSentrySdk(o =>
            {
                o.Dsn = sourceDsn;
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: sourceEventCapture));
            });

            var nextEventCapture = new TestEventCapture();
            using var secondDisposable = InitSentrySdk(nextEventCapture); // uses the default test DSN
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();
            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.TestException));

            Assert.AreEqual(0, sourceEventCapture.Count, sourceDsn);
            Assert.AreEqual(1, nextEventCapture.Count);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_DebugLogException_IsMarkedUnhandled()
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var testEventCapture = new TestEventCapture();
            using var _ = InitSentrySdk(testEventCapture);
            var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

            // act
            testBehaviour.gameObject.SendMessage(nameof(testBehaviour.DebugLogException));

            // assert
            Assert.NotNull(testEventCapture.First.SentryExceptions.SingleOrDefault(exception =>
                    exception.Mechanism?.Handled is false) is not null);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_DebugLogError_IsCaptured()
        {
            yield return BugFarmScene_DebugLog(inTask: false, logException: false);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_DebugLogError_IsCapturedInTask()
        {
            yield return BugFarmScene_DebugLog(inTask: true, logException: false);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_DebugLogException_IsCaptured()
        {
            yield return BugFarmScene_DebugLog(inTask: false, logException: true);
        }

        [UnityTest]
        public IEnumerator BugFarmScene_DebugLogException_IsCapturedInTask()
        {
            yield return BugFarmScene_DebugLog(inTask: true, logException: true);
        }

        // Note: can't use nunit TestCase() because it's not supported with IEnumerator return.
        private IEnumerator BugFarmScene_DebugLog(bool inTask, bool logException)
        {
            yield return SetupSceneCoroutine("1_BugFarm");

            // arrange
            var testEventCapture = new TestEventCapture();

            using (var _ = InitSentrySdk(testEventCapture))
            {
                var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

                // act
                var method = logException
                    ? (inTask ? nameof(testBehaviour.DebugLogExceptionInTask) : nameof(testBehaviour.DebugLogException))
                    : (inTask ? nameof(testBehaviour.DebugLogErrorInTask) : nameof(testBehaviour.DebugLogError));

                UnityEngine.Debug.Log("Triggering event throught he UI");
                testBehaviour.gameObject.SendMessage(method);

                Assert.True(testEventCapture.WaitOne(TimeSpan.FromSeconds(5)));
            }

            Assert.AreEqual(1, testEventCapture.Count);
            var isMainThread = testEventCapture.First.Tags.SingleOrDefault(t => t.Key == "unity.is_main_thread");
            if (isMainThread.Value is null)
            {
                UnityEngine.Debug.LogWarning("Event is missing the thread tag. "
                    + $"Message: {testEventCapture.First.Message}. Exception: {testEventCapture.First.Exception}");
            }
            Assert.AreEqual((!inTask).ToString(), isMainThread.Value);
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

        internal static IDisposable InitSentrySdk(TestEventCapture testEventCapture) => InitSentrySdk(o =>
            {
                o.AddIntegration(new UnityApplicationLoggingIntegration(eventCapture: testEventCapture));
            });

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
        private AutoResetEvent _eventReceived = new AutoResetEvent(false);

        public SentryEvent First
        {
            get
            {
                lock (_events)
                {
                    return _events.First();
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_events)
                {
                    return _events.Count();
                }
            }
        }
        public SentryId Capture(SentryEvent sentryEvent)
        {
            lock (_events)
            {
                _events.Add(sentryEvent);
                _eventReceived.Set();
                return sentryEvent.EventId;
            }
        }

        public bool WaitOne(TimeSpan timeout) => _eventReceived.WaitOne(timeout);
    }
}
