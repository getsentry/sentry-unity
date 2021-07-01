using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.SceneManagement;
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

            var expectedRequestCount = 5;
            var evt = new ManualResetEventSlim();
            var requests = new List<string>();

            using var _ = TestSentryUnity.Init(o =>
            {
                // o.AutoSessionTracking = true; We expect this to be true by default
                o.AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10);
                o.Debug = true;
                o.DiagnosticLevel = SentryLevel.Info;
            }, r =>
            {
                var request = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.Log(request);

                requests.Add(request);

                if (requests.Count >= expectedRequestCount)
                {
                    evt.Set();
                }
            });

            var sentryGameObject = GameObject.Find("SentryMonoBehaviour");
            var sentryMonoBehaviour = sentryGameObject.GetComponent<SentryMonoBehaviour>();

            sentryMonoBehaviour.OnApplicationFocus(false);
            yield return new WaitForSeconds(1f);
            sentryMonoBehaviour.OnApplicationFocus(true);

            if (!evt.Wait(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("Timeout");
            }

            // TODO: Use the envelope to parse the request and check for session

            Assert.AreEqual(expectedRequestCount, requests.Count);
        }

    }
}
