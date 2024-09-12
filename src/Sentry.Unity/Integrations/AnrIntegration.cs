using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity;

internal class AnrIntegration : ISdkIntegration
{
    private static readonly object Lock = new();
    private static AnrWatchDog? Watchdog;
    private readonly SentryMonoBehaviour _monoBehaviour;

    public AnrIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = (SentryUnityOptions)sentryOptions;
        lock (Lock)
        {
            if (Watchdog is null)
            {
                if (options.MultiThreading)
                {
                    Watchdog = new AnrWatchDogMultiThreaded(options.DiagnosticLogger,
                        _monoBehaviour,
                        options.AnrTimeout);
                }
                else
                {
                    Watchdog = new AnrWatchDogSingleThreaded(options.DiagnosticLogger,
                        _monoBehaviour,
                        options.AnrTimeout);
                }
            }
        }
        Watchdog.OnApplicationNotResponding += (_, e) => hub.CaptureException(e);
    }
}

internal abstract class AnrWatchDog
{
    protected readonly int DetectionTimeoutMs;
    // Note: we don't sleep for the whole detection timeout or we wouldn't capture if the ANR started later.
    protected readonly int SleepIntervalMs;
    protected readonly IDiagnosticLogger? Logger;
    protected readonly SentryMonoBehaviour MonoBehaviour;
    internal event EventHandler<ApplicationNotRespondingException> OnApplicationNotResponding = delegate { };
    protected bool Paused { get; private set; } = false;

    internal AnrWatchDog(IDiagnosticLogger? logger, SentryMonoBehaviour monoBehaviour, TimeSpan detectionTimeout)
    {
        MonoBehaviour = monoBehaviour;
        Logger = logger;
        DetectionTimeoutMs = (int)detectionTimeout.TotalMilliseconds;
        SleepIntervalMs = Math.Max(1, DetectionTimeoutMs / 5);

        MonoBehaviour.ApplicationPausing += () => Paused = true;
        MonoBehaviour.ApplicationResuming += () => Paused = false;

        // Stop when the app is being shut down.
        MonoBehaviour.Application.Quitting += () => Stop();
    }

    internal abstract void Stop(bool wait = false);

    protected void Report()
    {
        // Don't report events while in the background.
        if (!Paused)
        {
            var message = $"Application not responding for at least {DetectionTimeoutMs} ms.";
            Logger?.LogInfo("Detected an ANR event: {0}", message);
            OnApplicationNotResponding?.Invoke(this, new ApplicationNotRespondingException(message));
        }
    }
}

internal class AnrWatchDogMultiThreaded : AnrWatchDog
{
    private int _ticksSinceUiUpdate; // how many _sleepIntervalMs have elapsed since the UI updated last time
    private bool _reported; // don't report the same ANR instance multiple times
    private bool _stop;
    private readonly Thread _thread = null!;

    internal AnrWatchDogMultiThreaded(IDiagnosticLogger? logger, SentryMonoBehaviour monoBehaviour, TimeSpan detectionTimeout)
        : base(logger, monoBehaviour, detectionTimeout)
    {
        _thread = new Thread(Run)
        {
            Name = "Sentry-ANR-WatchDog",
            IsBackground = true, // do not block on app shutdown
            Priority = System.Threading.ThreadPriority.BelowNormal,
        };
        _thread.Start();

        // Update the UI status periodically by running a coroutine on the UI thread
        MonoBehaviour.StartCoroutine(UpdateUiStatus());
    }

    internal override void Stop(bool wait = false)
    {
        _stop = true;
        if (wait)
        {
            _thread.Join();
        }
    }

    private IEnumerator UpdateUiStatus()
    {
        var waitForSeconds = new WaitForSecondsRealtime((float)SleepIntervalMs / 1000);

        yield return waitForSeconds;
        while (!_stop)
        {
            _ticksSinceUiUpdate = 0;
            _reported = false;
            yield return waitForSeconds;
        }
    }

    private void Run()
    {
        try
        {
            var reportThreshold = DetectionTimeoutMs / SleepIntervalMs;

            Logger?.Log(SentryLevel.Info,
                "Starting an ANR WatchDog - detection timeout: {0} ms, check every {1} ms => report after {2} failed checks",
                null, DetectionTimeoutMs, SleepIntervalMs, reportThreshold);

            while (!_stop)
            {
                _ticksSinceUiUpdate++;
                Thread.Sleep(SleepIntervalMs);

                if (Paused)
                {
                    _ticksSinceUiUpdate = 0;
                }
                else if (_ticksSinceUiUpdate >= reportThreshold && !_reported)
                {
                    Report();
                    _reported = true;
                }
            }
        }
        catch (ThreadAbortException e)
        {
            Logger?.Log(SentryLevel.Debug, "ANR watchdog thread aborted.", e);
        }
        catch (Exception e)
        {
            Logger?.Log(SentryLevel.Error, "Exception in the ANR watchdog.", e);
        }
    }
}

internal class AnrWatchDogSingleThreaded : AnrWatchDog
{
    private readonly Stopwatch _watch = new();
    private bool _stop;

    internal AnrWatchDogSingleThreaded(IDiagnosticLogger? logger, SentryMonoBehaviour monoBehaviour, TimeSpan detectionTimeout)
        : base(logger, monoBehaviour, detectionTimeout)
    {
        // Check the UI status periodically by running a coroutine on the UI thread and checking the elapsed time
        _watch.Start();
        MonoBehaviour.StartCoroutine(UpdateUiStatus());
    }

    internal override void Stop(bool wait = false) => _stop = true;

    private IEnumerator UpdateUiStatus()
    {
        var waitForSeconds = new WaitForSecondsRealtime((float)SleepIntervalMs / 1000);
        while (!_stop)
        {
            if (_watch.ElapsedMilliseconds >= DetectionTimeoutMs)
            {
                Report();
            }
            _watch.Restart();
            yield return waitForSeconds;
        }
    }
}
