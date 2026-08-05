using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Unity.Metrics;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class SentryMonoBehaviourTests
{
    private sealed class CountingMetricMonitor : IGameMetricMonitor
    {
        public int SampleCount { get; private set; }

        public void Sample() => SampleCount++;
    }

    private class Fixture
    {
        public SentryMonoBehaviour GetSut()
        {
            var gameObject = new GameObject("PauseTest");
            var sentryMonoBehaviour = gameObject.AddComponent<SentryMonoBehaviour>();
            sentryMonoBehaviour.Application = new TestApplication();

            return sentryMonoBehaviour;
        }
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [Test]
    public void OnApplicationPause_PauseStatusTrue_ApplicationPausingInvoked()
    {
        var wasPausingCalled = false;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => wasPausingCalled = true;

        sut.OnApplicationPause(true);

        Assert.IsTrue(wasPausingCalled);
    }

    [Test]
    public void OnApplicationFocus_FocusFalse_ApplicationPausingInvoked()
    {
        var wasPausingCalled = false;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => wasPausingCalled = true;

        sut.OnApplicationFocus(false);

        Assert.IsTrue(wasPausingCalled);
    }

    [Test]
    public void UpdatePauseStatus_PausedTwice_ApplicationPausingInvokedOnlyOnce()
    {
        var counter = 0;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => counter++;

        sut.UpdatePauseStatus(true);
        sut.UpdatePauseStatus(true);

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void UpdatePauseStatus_ResumedTwice_ApplicationResumingInvokedOnlyOnce()
    {
        var counter = 0;

        var sut = _fixture.GetSut();
        sut.ApplicationResuming += () => counter++;
        // We need to pause it first to resume it.
        sut.UpdatePauseStatus(true);

        sut.UpdatePauseStatus(false);
        sut.UpdatePauseStatus(false);

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void QueueCoroutine_CalledOnMainThread_StartsCoroutineImmediately()
    {
        var sut = _fixture.GetSut();
        var coroutineExecuted = false;

        IEnumerator TestCoroutine()
        {
            coroutineExecuted = true;
            yield return null;
        }

        sut.QueueCoroutine(TestCoroutine());

        Assert.IsTrue(coroutineExecuted);
    }

    [UnityTest]
    public IEnumerator QueueCoroutine_QueuedOnBackgroundThread_StartsInUpdate()
    {
        var sut = _fixture.GetSut();
        var coroutineExecuted = false;

        IEnumerator TestCoroutine()
        {
            coroutineExecuted = true;
            yield return null;
        }

        var thread = new Thread(() =>
        {
            sut.QueueCoroutine(TestCoroutine());
        });

        thread.Start();
        thread.Join();

        // Coroutine should not have started yet
        Assert.IsFalse(coroutineExecuted);

        // Wait for the coroutine to execute - trigger `Update`
        yield return null;

        Assert.IsTrue(coroutineExecuted);
    }

    [UnityTest]
    public IEnumerator StartMetricsMonitor_PerFrame_SamplesAfterFirstFrame()
    {
        var sut = _fixture.GetSut();
        var monitor = new CountingMetricMonitor();

        sut.StartMetricsMonitor(monitor);

        Assert.AreEqual(0, monitor.SampleCount);

        yield return null;

        Assert.AreEqual(1, monitor.SampleCount);
    }

    [UnityTest]
    public IEnumerator StartMetricsMonitor_Interval_SamplesAfterInterval()
    {
        var sut = _fixture.GetSut();
        var monitor = new CountingMetricMonitor();

        sut.StartMetricsMonitor(monitor, System.TimeSpan.FromSeconds(0.1));

        yield return null;

        Assert.AreEqual(0, monitor.SampleCount);

        yield return new WaitForSecondsRealtime(0.1f);

        Assert.AreEqual(1, monitor.SampleCount);
    }
}
