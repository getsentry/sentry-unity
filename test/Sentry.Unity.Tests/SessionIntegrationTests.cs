using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

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
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880";
            configure?.Invoke(options);
        });

        return new SentryDisposable();
    }

    private sealed class SentryDisposable : IDisposable
    {
        public void Dispose() => SentrySdk.Close();
    }
}
