using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Sentry.Extensibility;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Tests;

public class AnrDetectionTests
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(0.5);
    private readonly IDiagnosticLogger _logger = new TestLogger(forwardToUnityLog: true);
    private GameObject _gameObject = null!;
    private SentryMonoBehaviour _monoBehaviour = null!;
    private AnrWatchDog _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("Tests");
        _monoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
    }

    [TearDown]
    public void TearDown() => _sut.Stop(wait: true);

    private AnrWatchDog CreateWatchDog(bool multiThreaded)
    {
        UnityEngine.Debug.Log($"Preparing ANR watchdog: timeout={_timeout} multiThreaded={multiThreaded}");
        return multiThreaded
            ? new AnrWatchDogMultiThreaded(_logger, _monoBehaviour, _timeout)
            : new AnrWatchDogSingleThreaded(_logger, _monoBehaviour, _timeout);
    }

    // Needed for [UnityTest] - IEnumerator return value
    // https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-tests-parameterized.html
    private static bool[] MultiThreadingTestValues = { true, false };

    [UnityTest]
    public IEnumerator DetectsStuckUI([ValueSource(nameof(MultiThreadingTestValues))] bool multiThreaded)
    {
        ApplicationNotResponding? arn = null;
        _sut = CreateWatchDog(multiThreaded);
        _sut.OnApplicationNotResponding += (_, e) => arn = e;

        // Thread.Sleep blocks the UI thread
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromTicks(_timeout.Ticks * 2) && arn is null)
        {
            Thread.Sleep(10);
        }

        // We need to let the single-threaded watchdog populate `arn` after UI became responsive again.
        if (!multiThreaded)
        {
            watch.Restart();
            while (watch.Elapsed < _timeout && arn is null)
            {
                yield return null;
            }
        }

        Assert.IsNotNull(arn);
        Assert.That(arn!.Message, Does.StartWith("Application not responding "));
    }

    [UnityTest]
    public IEnumerator DoesntReportWorkingUI([ValueSource(nameof(MultiThreadingTestValues))] bool multiThreaded)
    {
        ApplicationNotResponding? arn = null;
        _sut = CreateWatchDog(multiThreaded);
        _sut.OnApplicationNotResponding += (_, e) => arn = e;

        // yield WaitForSeconds doesn't block the UI thread
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromTicks(_timeout.Ticks * 3))
        {
            yield return new WaitForSeconds(0.01f);
        }

        Assert.IsNull(arn);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void DoesntReportShortlyStuckUI(bool multiThreaded)
    {
        ApplicationNotResponding? arn = null;
        _sut = CreateWatchDog(multiThreaded);
        _sut.OnApplicationNotResponding += (_, e) => arn = e;

        // Thread.Sleep blocks the UI thread
        Thread.Sleep(TimeSpan.FromTicks(_timeout.Ticks / 2));

        Assert.IsNull(arn);
    }

    [UnityTest]
    public IEnumerator DoesntReportWhilePaused([ValueSource(nameof(MultiThreadingTestValues))] bool multiThreaded)
    {
        ApplicationNotResponding? arn = null;
        _sut = CreateWatchDog(multiThreaded);
        _sut.OnApplicationNotResponding += (_, e) => arn = e;

        // mark the app as paused
        _monoBehaviour.UpdatePauseStatus(true);

        // Thread.Sleep blocks the UI thread
        Thread.Sleep(TimeSpan.FromTicks(_timeout.Ticks * 2));

        // We need to let the single-threaded watchdog populate `arn` after UI became responsive again.
        if (!multiThreaded)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < _timeout && arn is null)
            {
                yield return null;
            }
        }

        // mark as resumed
        _monoBehaviour.UpdatePauseStatus(false);

        Assert.IsNull(arn);
    }

    [UnityTest]
    public IEnumerator IsNotAffectedByTimeScale()
    {
        ApplicationNotResponding? anr = null;
        _sut = CreateWatchDog(true);
        _sut.OnApplicationNotResponding += (_, e) => anr = e;

        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime((float)_timeout.TotalSeconds * 2);
        Time.timeScale = 1.0f;

        Assert.IsNull(anr);
    }
}