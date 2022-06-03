using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    internal class ANRIntegration : ISdkIntegration
    {
        private static readonly object _lock = new object();
        private static ANRWatchDog? _watchdog;
        private SentryMonoBehaviour _monoBehaviour;

        public ANRIntegration(SentryMonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
        }

        public void Register(IHub hub, SentryOptions sentryOptions)
        {
            var options = (SentryUnityOptions)sentryOptions;
            lock (_lock)
            {
                if (_watchdog is null)
                {
                    if (options.MultiThreading)
                    {
                        _watchdog = new ANRWatchDogMultiThreaded(options.DiagnosticLogger, _monoBehaviour);
                    }
                    else
                    {
                        _watchdog = new ANRWatchDogSingleThreaded(options.DiagnosticLogger, _monoBehaviour);
                    }
                }
            }
            _watchdog.OnApplicationNotResponding += (_, e) => hub.CaptureException(e);
        }
    }

    internal abstract class ANRWatchDog
    {
        protected readonly int _detectionTimeoutMs;
        // Note: we don't sleep for the whole detection timeout or we wouldn't capture if the ANR started later.
        protected readonly int _sleepIntervalMs;
        protected readonly IDiagnosticLogger? _logger;
        protected readonly SentryMonoBehaviour _monoBehaviour;
        internal event EventHandler<ApplicationNotResponding> OnApplicationNotResponding = delegate { };

        internal ANRWatchDog(IDiagnosticLogger? logger, SentryMonoBehaviour monoBehaviour, int detectionTimeoutMilliseconds = 5000)
        {
            _monoBehaviour = monoBehaviour;
            _logger = logger;
            _detectionTimeoutMs = detectionTimeoutMilliseconds;
            _sleepIntervalMs = Math.Max(1, _detectionTimeoutMs / 5);

            // Stop when the app is being shut down.
            _monoBehaviour.Application.Quitting += () => Stop();
        }

        abstract internal void Stop(bool wait = false);

        protected void Report()
        {
            var message = $"Application not responding for at least {_detectionTimeoutMs} ms.";
            _logger?.LogInfo("Detected an ANR event: {0}", message);
            OnApplicationNotResponding?.Invoke(this, new ApplicationNotResponding(message));
        }
    }

    internal class ANRWatchDogMultiThreaded : ANRWatchDog
    {
        private int _ticksSinceUiUpdate = 0; // how many _sleepIntervalMs have elapsed since the UI updated last time
        private bool _reported = false; // don't report the same ANR instance multiple times
        private bool _stop = false;
        private readonly Thread _thread = null!;

        internal ANRWatchDogMultiThreaded(IDiagnosticLogger? logger, SentryMonoBehaviour monoBehaviour, int detectionTimeoutMilliseconds = 5000)
          : base(logger, monoBehaviour, detectionTimeoutMilliseconds)
        {
            _thread = new Thread(Run)
            {
                Name = "Sentry-ANR-WatchDog",
                IsBackground = true, // do not block on app shutdown
                Priority = System.Threading.ThreadPriority.BelowNormal,
            };
            _thread.Start();

            // Update the UI status periodically by running a couroutine on the UI thread
            _monoBehaviour.StartCoroutine(UpdateUiStatus());
        }

        override internal void Stop(bool wait = false)
        {
            _stop = true;
            if (wait)
            {
                _thread.Join();
            }
        }

        private IEnumerator UpdateUiStatus()
        {
            var waitForSeconds = new WaitForSeconds((float)_sleepIntervalMs / 1000);

            yield return waitForSeconds;
            while (!_stop)
            {
                // _logger?.LogDebug("ANR WatchDog - notifying that UI is responsive");
                _ticksSinceUiUpdate = 0;
                _reported = false;
                yield return waitForSeconds;
            }
        }

        private void Run()
        {
            try
            {
                int ticksSinceUiUpdate; // avoiding allocs in the loop
                int reportTreshold = _detectionTimeoutMs / _sleepIntervalMs;

                _logger?.Log(SentryLevel.Info,
                    "Starting an ANR WatchDog - detection timeout: {0} ms, check every {1} ms => report after {2} failed checks",
                    null, _detectionTimeoutMs, _sleepIntervalMs, reportTreshold);

                while (!_stop)
                {
                    _ticksSinceUiUpdate++;
                    Thread.Sleep(_sleepIntervalMs);

                    // time to check
                    ticksSinceUiUpdate = _ticksSinceUiUpdate;
                    // _logger?.LogDebug("ANR status: {0} failed checks since last UI update", ticksSinceUiUpdate);
                    if (ticksSinceUiUpdate >= reportTreshold && !_reported)
                    {
                        Report();
                        _reported = true;
                    }
                }
            }
            catch (ThreadAbortException e)
            {
                _logger?.Log(SentryLevel.Debug, "ANR watchdog thread aborted.", e);
            }
            catch (Exception e)
            {
                _logger?.Log(SentryLevel.Error, "Exception in the ANR watchdog.", e);
            }
        }
    }

    internal class ANRWatchDogSingleThreaded : ANRWatchDog
    {
        private Stopwatch _watch = new Stopwatch();
        private bool _stop = false;

        internal ANRWatchDogSingleThreaded(IDiagnosticLogger? logger, SentryMonoBehaviour monoBehaviour, int detectionTimeoutMilliseconds = 5000)
         : base(logger, monoBehaviour, detectionTimeoutMilliseconds)
        {
            // Check the UI status periodically by running a couroutine on the UI thread and checking the elapsed time
            //
            _watch.Start();
            _monoBehaviour.StartCoroutine(UpdateUiStatus());
        }

        override internal void Stop(bool wait = false) =>
            _stop = true;

        private IEnumerator UpdateUiStatus()
        {
            var waitForSeconds = new WaitForSeconds((float)_sleepIntervalMs / 1000);
            while (!_stop)
            {
                if (_watch.ElapsedMilliseconds >= _detectionTimeoutMs)
                {
                    Report();
                }
                _watch.Restart();
                yield return waitForSeconds;
            }
        }
    }

    internal class ApplicationNotResponding : Exception
    {
        internal ApplicationNotResponding() : base() { }
        internal ApplicationNotResponding(string message) : base(message) { }
        internal ApplicationNotResponding(string message, Exception innerException) : base(message, innerException) { }
    }
}
