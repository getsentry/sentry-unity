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
    public IEnumerator StartAppHangHeartbeat_ArmsAfterFirstFrameThenFiresPeriodically()
    {
        var sut = GetSut();
        var count = 0;

        // Use a tiny interval so the test runs fast.
        sut.StartAppHangHeartbeat(() => count++, TimeSpan.FromSeconds(0.05));

        // Arming is deferred until the player loop ticks, so nothing fires synchronously on start.
        Assert.AreEqual(0, count);

        // The first heartbeat fires once a frame has passed.
        yield return null;
        Assert.GreaterOrEqual(count, 1);

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

        // Let it arm (deferred until the player loop ticks).
        yield return null;
        Assert.GreaterOrEqual(count, 1);

        UnityEngine.Object.DestroyImmediate(sut.gameObject);
        var countAfterDestroy = count;

        yield return new WaitForSecondsRealtime(0.12f);

        Assert.AreEqual(countAfterDestroy, count);
    }
}
