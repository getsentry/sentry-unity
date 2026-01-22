using System.Collections;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class LifeCycleIntegrationTests
{
    [TearDown]
    public void TearDown()
    {
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.Close();
        }
    }

    [UnityTest]
    public IEnumerator SessionIntegration_Init_SentryMonoBehaviourCreated()
    {
        yield return null;

        var options = new SentryUnityOptions(application: new TestApplication())
        {
            Dsn = SentryTests.TestDsn
            // AutoSessionTracking = true; We expect this to be true by default
        };
        SentrySdk.Init(options);

        var sentryGameObject = GameObject.Find("SentryMonoBehaviour");
        var sentryMonoBehaviour = sentryGameObject.GetComponent<SentryMonoBehaviour>();

        Assert.IsNotNull(sentryGameObject);
        Assert.IsNotNull(sentryMonoBehaviour);
    }
}
