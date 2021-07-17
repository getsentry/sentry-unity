using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public class SentryMonoBehaviourTests
    {
        private class Fixture
        {
            public SentryMonoBehaviour GetSut(RuntimePlatform platform)
            {
                var gameObject = new GameObject("PauseTest");
                var sentryMonoBehaviour = gameObject.AddComponent<SentryMonoBehaviour>();
                sentryMonoBehaviour.Application = new TestApplication(platform: platform);

                return sentryMonoBehaviour;
            }
        }

        private Fixture _fixture = null!;

        [SetUp]
        public void SetUp() => _fixture = new Fixture();

        [Test]
        public void OnApplicationPause_OnAndroid_ApplicationPausingTriggered()
        {
            var wasPausingCalled = false;

            var sut = _fixture.GetSut(RuntimePlatform.Android);
            sut.ApplicationPausing += () => wasPausingCalled = true;

            sut.OnApplicationPause(true);

            Assert.IsTrue(wasPausingCalled);
        }

        [Test]
        public void OnApplicationPause_NotOnAndroid_ApplicationPausingNotTriggered()
        {
            var wasPausingCalled = false;

            var sut = _fixture.GetSut(RuntimePlatform.IPhonePlayer);
            sut.ApplicationPausing += () => wasPausingCalled = true;

            sut.OnApplicationPause(true);

            Assert.IsFalse(wasPausingCalled);
        }

        [Test]
        public void OnApplicationPause_OnAndroid_ApplicationPausingTriggeredOnlyOnce()
        {
            var pauseEventTriggerCounter = 0;

            var sut = _fixture.GetSut(RuntimePlatform.Android);
            sut.ApplicationPausing += () => pauseEventTriggerCounter++;

            sut.OnApplicationPause(true);
            sut.OnApplicationPause(true);

            Assert.AreEqual(1, pauseEventTriggerCounter);
        }

        [Test]
        public void OnApplicationPause_OnAndroid_PausingIsRequiredBeforeApplicationResumingTrigger()
        {
            var wasPausingCalled = false;
            var wasResumingCalled = false;

            var sut = _fixture.GetSut(RuntimePlatform.Android);
            sut.ApplicationPausing += () => wasPausingCalled = true;
            sut.ApplicationResuming += () => wasResumingCalled = true;

            sut.OnApplicationPause(false);

            Assert.IsFalse(wasResumingCalled);

            sut.OnApplicationPause(true);
            sut.OnApplicationPause(false);

            Assert.IsTrue(wasPausingCalled);
            Assert.IsTrue(wasResumingCalled);
        }

        [Test]
        public void OnApplicationFocus_OnAndroid_ApplicationPausingNotTriggered()
        {
            var wasPausingCalled = false;

            var sut = _fixture.GetSut(RuntimePlatform.Android);
            sut.ApplicationPausing += () => wasPausingCalled = true;

            sut.OnApplicationFocus(false);

            Assert.IsFalse(wasPausingCalled);
        }

        [Test]
        public void OnApplicationFocus_NotOnAndroid_ApplicationPausingTriggered()
        {
            var wasPausingCalled = false;

            var sut = _fixture.GetSut(RuntimePlatform.IPhonePlayer);
            sut.ApplicationPausing += () => wasPausingCalled = true;

            sut.OnApplicationFocus(false);

            Assert.IsTrue(wasPausingCalled);
        }

        [Test]
        public void OnApplicationFocus_NotOnAndroid_ApplicationPausingTriggeredOnlyOnce()
        {
            var pauseEventTriggerCounter = 0;

            var sut = _fixture.GetSut(RuntimePlatform.IPhonePlayer);
            sut.ApplicationPausing += () => pauseEventTriggerCounter++;

            sut.OnApplicationFocus(false);
            sut.OnApplicationFocus(false);

            Assert.AreEqual(1, pauseEventTriggerCounter);
        }

        [Test]
        public void OnApplicationFocus_NotOnAndroid_PausingIsRequiredBeforeApplicationResumingTrigger()
        {
            var wasPausingCalled = false;
            var wasResumingCalled = false;

            var sut = _fixture.GetSut(RuntimePlatform.IPhonePlayer);
            sut.ApplicationPausing += () => wasPausingCalled = true;
            sut.ApplicationResuming += () => wasResumingCalled = true;

            sut.OnApplicationFocus(true);

            Assert.IsFalse(wasResumingCalled);

            sut.OnApplicationFocus(false);
            sut.OnApplicationFocus(true);

            Assert.IsTrue(wasPausingCalled);
            Assert.IsTrue(wasResumingCalled);
        }
    }
}
