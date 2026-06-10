using System;
using System.Collections;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class SentryMonoBehaviourAppHangTests
{
    private SentryMonoBehaviour GetSut()
    {
        var gameObject = new GameObject("AppHangTest");
        var sut = gameObject.AddComponent<SentryMonoBehaviour>();
        sut.Application = new TestApplication();
        return sut;
    }

    [UnityTest]
    public IEnumerator StartAppHangHeartbeat_FiresImmediatelyThenPeriodically()
    {
        var sut = GetSut();
        var count = 0;

        // Use a tiny interval so the test runs fast.
        sut.StartAppHangHeartbeat(() => count++, TimeSpan.FromSeconds(0.05));

        // First heartbeat fires synchronously on start.
        Assert.AreEqual(1, count);

        // After roughly two intervals we expect at least two more.
        yield return new WaitForSecondsRealtime(0.12f);

        Assert.GreaterOrEqual(count, 3);
    }

    [UnityTest]
    public IEnumerator StartAppHangHeartbeat_StopsWhenObjectDestroyed()
    {
        var sut = GetSut();
        var count = 0;

        sut.StartAppHangHeartbeat(() => count++, TimeSpan.FromSeconds(0.05));
        Assert.AreEqual(1, count);

        UnityEngine.Object.DestroyImmediate(sut.gameObject);
        var countAfterDestroy = count;

        yield return new WaitForSecondsRealtime(0.12f);

        Assert.AreEqual(countAfterDestroy, count);
    }
}
