using System;
using System.Collections;
using NUnit.Framework;
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

            using var _ = InitSentrySdk(o =>
            {
                // o.AutoSessionTracking = true; We expect this to be true by default
            });

            var sentryGameObject = GameObject.Find("SentryMonoBehaviour");
            var sentryMonoBehaviour = sentryGameObject.GetComponent<SentryMonoBehaviour>();

            Assert.IsNotNull(sentryGameObject);
            Assert.IsNotNull(sentryMonoBehaviour);
        }

        internal IDisposable InitSentrySdk(Action<SentryUnityOptions>? configure = null)
        {
            SentryUnity.Init(options =>
            {
                options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
                configure?.Invoke(options);
            });

            return new SentryDisposable();
        }

        private sealed class SentryDisposable : IDisposable
        {
            public void Dispose() => SentrySdk.Close();
        }
    }
}
