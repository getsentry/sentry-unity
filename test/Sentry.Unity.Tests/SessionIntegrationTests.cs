using System;
using System.Collections;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public class SessionIntegrationTests
    {
        [UnityTest]
        public IEnumerator SessionIntegration_Init_SentryMonoBehaviourCreated()
        {
            yield return null;

            using var _ = TestSentryUnity.Init(o =>
            {
                // o.AutoSessionTracking = true; We expect this to be true by default
            });

            var sentryGameObject = GameObject.Find("SentryMonoBehaviour");
            var sentryMonoBehaviour = sentryGameObject.GetComponent<SentryMonoBehaviour>();

            Assert.IsNotNull(sentryGameObject);
            Assert.IsNotNull(sentryMonoBehaviour);
        }

        [UnityTest]
        public IEnumerator SessionIntegration_SomethingElse()
        {
            yield return null;
            var request = string.Empty;

            using var _ = TestSentryUnity.Init(o =>
            {
                // o.AutoSessionTracking = true; We expect this to be true by default
                o.AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10);
            }, r =>
            {
                request = r.Content.ReadAsStringAsync().Result;
                Debug.Log(request);
            });

            // var sentryGameObject = GameObject.Find("SentryMonoBehaviour");
            // var sentryMonoBehaviour = sentryGameObject.GetComponent<SentryMonoBehaviour>();
            //
            // Debug.LogError("testerror");
            //
            // sentryMonoBehaviour.OnApplicationFocus(false);
            // yield return new WaitForSeconds(1f);
            // sentryMonoBehaviour.OnApplicationFocus(true);

            Assert.AreNotEqual(string.Empty, request);
        }
    }
}
