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

    [UnityTest]
    public IEnumerator StartAppHangHeartbeat_SkipsHeartbeatsWhilePaused()
    {
        var sut = GetSut();
        var count = 0;

        sut.StartAppHangHeartbeat(() => count++, TimeSpan.FromSeconds(0.05));
        yield return null;

        sut.UpdatePauseStatus(true);
        var countWhilePaused = count;
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.AreEqual(countWhilePaused, count);

        sut.UpdatePauseStatus(false);
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.Greater(count, countWhilePaused);
    }

    [UnityTest]
    public IEnumerator StartAppHangHeartbeat_BeatsRightAwayOnResume()
    {
        var sut = GetSut();
        var count = 0;

        sut.StartAppHangHeartbeat(() => count++, () => { });
        yield return null;

        sut.UpdatePauseStatus(true);
        var countWhilePaused = count;
        sut.UpdatePauseStatus(false);

        Assert.AreEqual(countWhilePaused + 1, count);
    }

    [UnityTest]
    public IEnumerator StartAppHangHeartbeat_RestartsHeartbeatsOnResume()
    {
        var sut = GetSut();
        var count = 0;

        var heartbeat = sut.StartAppHangHeartbeat(() => count++, () => { }, TimeSpan.FromSeconds(0.05));
        yield return null;

        // Like a reload of the player, which leaves the suspended coroutine stalled.
        sut.UpdatePauseStatus(true);
        sut.StopCoroutine(heartbeat);
        sut.UpdatePauseStatus(false);
        var countAfterResume = count;
        yield return new WaitForSecondsRealtime(0.12f);

        Assert.Greater(count, countAfterResume);
    }

    [Test]
    public void StartAppHangHeartbeat_DoesNotArmOnResumeBeforeFirstHeartbeat()
    {
        var sut = GetSut();
        var count = 0;

        sut.StartAppHangHeartbeat(() => count++, () => { });
        sut.UpdatePauseStatus(true);
        sut.UpdatePauseStatus(false);

        Assert.AreEqual(0, count);
    }

    [Test]
    public void StartAppHangHeartbeat_PausesNativeDetectorWhenApplicationPauses()
    {
        var sut = GetSut();
        var pauses = 0;

        sut.StartAppHangHeartbeat(() => { }, () => pauses++);

        sut.UpdatePauseStatus(true);
        Assert.AreEqual(1, pauses);

        sut.UpdatePauseStatus(false);
        sut.UpdatePauseStatus(true);
        Assert.AreEqual(2, pauses);
    }
}
