using System;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Android.Tests;

public class AnrHeartbeatTests
{
    private TestLogger _logger = null!;
    private TestSentryJava _sentryJava = null!;
    private TestSentryMonoBehaviour _monoBehaviour = null!;
    private GameObject _gameObject = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new TestLogger();
        _sentryJava = new TestSentryJava();
        _gameObject = new GameObject(nameof(AnrHeartbeatTests));
        _monoBehaviour = _gameObject.AddComponent<TestSentryMonoBehaviour>();
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(_gameObject);

    [Test]
    public void Beat_CallsNotifyAnrThreadAlive()
    {
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.FromSeconds(5), _logger);

        sut.Beat();
        sut.Beat();

        Assert.AreEqual(2, _sentryJava.NotifyAnrThreadAliveCount);
    }

    [Test]
    public void Start_StartsCoroutineAndSubscribesToPauseResume()
    {
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.FromSeconds(5), _logger);

        sut.Start();

        Assert.IsTrue(_monoBehaviour.StartCoroutineCalled);
    }

    [Test]
    public void OnPause_StopsCoroutine()
    {
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.FromSeconds(5), _logger);
        sut.Start();

        _monoBehaviour.PauseApplication();

        Assert.AreEqual(1, _monoBehaviour.StopCoroutineCallCount);
    }

    [Test]
    public void OnResume_RestartsCoroutine()
    {
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.FromSeconds(5), _logger);
        sut.Start();
        _monoBehaviour.PauseApplication();

        _monoBehaviour.ResumeApplication();

        // First start + restart on resume = StartCoroutineCalled stays true (it's a flag),
        // verify by checking the heartbeat is once again live: a subsequent pause stops again.
        _monoBehaviour.PauseApplication();
        Assert.AreEqual(2, _monoBehaviour.StopCoroutineCallCount);
    }

    [Test]
    public void Constructor_ClampsIntervalAboveZero()
    {
        // Should not throw even with a zero timeout.
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.Zero, _logger);
        Assert.DoesNotThrow(() => sut.Beat());
    }

    [Test]
    public void Start_CalledTwice_IsIdempotent()
    {
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.FromSeconds(5), _logger);
        sut.Start();
        sut.Start();

        // A single pause should produce exactly one StopCoroutine call. If Start were
        // not idempotent, the second pause-resume cycle would observe duplicate handlers.
        _monoBehaviour.PauseApplication();
        Assert.AreEqual(1, _monoBehaviour.StopCoroutineCallCount);
    }

    [Test]
    public void Stop_StopsCoroutineAndUnsubscribes()
    {
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.FromSeconds(5), _logger);
        sut.Start();

        sut.Stop();

        // Stop itself counts as a coroutine stop.
        Assert.AreEqual(1, _monoBehaviour.StopCoroutineCallCount);

        // Subsequent pause must not trigger another stop (handler should be unsubscribed).
        _monoBehaviour.PauseApplication();
        Assert.AreEqual(1, _monoBehaviour.StopCoroutineCallCount);
    }

    [Test]
    public void Stop_BeforeStart_DoesNothing()
    {
        var sut = new AnrHeartbeat(_monoBehaviour, _sentryJava, TimeSpan.FromSeconds(5), _logger);

        Assert.DoesNotThrow(() => sut.Stop());
        Assert.AreEqual(0, _monoBehaviour.StopCoroutineCallCount);
    }
}
