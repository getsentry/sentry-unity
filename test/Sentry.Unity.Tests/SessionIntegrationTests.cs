using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.TestBehaviours;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public class SessionIntegrationTests
    {
            [UnityTest]
            public IEnumerator SessionIntegration_SentryMonoBehaviourCreated()
            {
                yield return null;

                using var _ = IntegrationTests.InitSentrySdk(o =>
                {
                    o.AutoSessionTracking = true;
                    o.AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10);
                });
                var testBehaviour = GameObject.FindObjectOfType<SentryMonoBehaviour>();

                Assert.IsNotNull(testBehaviour);
            }
    }
}
