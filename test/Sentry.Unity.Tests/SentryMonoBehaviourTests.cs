using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public class SentryMonoBehaviourTests
    {
        [Test]
        public void OnApplicationPause_OnAndroid_ApplicationPausingTriggered()
        {
            var wasPausingCalled = false;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.Android);
            listener.ApplicationPausing += () => wasPausingCalled = true;

            listener.OnApplicationPause(true);

            Assert.IsTrue(wasPausingCalled);
        }

        [Test]
        public void OnApplicationPause_NotOnAndroid_ApplicationPausingNotTriggered()
        {
            var wasPausingCalled = false;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.IPhonePlayer);
            listener.ApplicationPausing += () => wasPausingCalled = true;

            listener.OnApplicationPause(true);

            Assert.IsFalse(wasPausingCalled);
        }

        [Test]
        public void OnApplicationPause_OnAndroid_ApplicationPausingTriggeredOnlyOnce()
        {
            var pauseEventTriggerCounter = 0;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.Android);
            listener.ApplicationPausing += () => pauseEventTriggerCounter++;

            listener.OnApplicationPause(true);
            listener.OnApplicationPause(true);

            Assert.AreEqual(1, pauseEventTriggerCounter);
        }

        [Test]
        public void OnApplicationPause_OnAndroid_PausingIsRequiredBeforeApplicationResumingTrigger()
        {
            var wasPausingCalled = false;
            var wasResumingCalled = false;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.Android);
            listener.ApplicationPausing += () => wasPausingCalled = true;
            listener.ApplicationResuming += () => wasResumingCalled = true;

            listener.OnApplicationPause(false);

            Assert.IsFalse(wasResumingCalled);

            listener.OnApplicationPause(true);
            listener.OnApplicationPause(false);

            Assert.IsTrue(wasPausingCalled);
            Assert.IsTrue(wasResumingCalled);
        }

        [Test]
        public void OnApplicationFocus_OnAndroid_ApplicationPausingNotTriggered()
        {
            var wasPausingCalled = false;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.Android);
            listener.ApplicationPausing += () => wasPausingCalled = true;

            listener.OnApplicationFocus(false);

            Assert.IsFalse(wasPausingCalled);
        }

        [Test]
        public void OnApplicationFocus_NotOnAndroid_ApplicationPausingTriggered()
        {
            var wasPausingCalled = false;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.IPhonePlayer);
            listener.ApplicationPausing += () => wasPausingCalled = true;

            listener.OnApplicationFocus(false);

            Assert.IsTrue(wasPausingCalled);
        }

        [Test]
        public void OnApplicationFocus_NotOnAndroid_ApplicationPausingTriggeredOnlyOnce()
        {
            var pauseEventTriggerCounter = 0;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.IPhonePlayer);
            listener.ApplicationPausing += () => pauseEventTriggerCounter++;

            listener.OnApplicationFocus(false);
            listener.OnApplicationFocus(false);

            Assert.AreEqual(1, pauseEventTriggerCounter);
        }

        [Test]
        public void OnApplicationFocus_NotOnAndroid_PausingIsRequiredBeforeApplicationResumingTrigger()
        {
            var wasPausingCalled = false;
            var wasResumingCalled = false;

            var gameObject = new GameObject("PauseTest");
            var listener = gameObject.AddComponent<SentryMonoBehaviour>();
            listener.Application = new TestApplication (platform: RuntimePlatform.IPhonePlayer);
            listener.ApplicationPausing += () => wasPausingCalled = true;
            listener.ApplicationResuming += () => wasResumingCalled = true;

            listener.OnApplicationFocus(true);

            Assert.IsFalse(wasResumingCalled);

            listener.OnApplicationFocus(false);
            listener.OnApplicationFocus(true);

            Assert.IsTrue(wasPausingCalled);
            Assert.IsTrue(wasResumingCalled);
        }
    }
}
