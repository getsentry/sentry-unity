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
        private static ANRWatchDog _watchdog = new ANRWatchDog();
        private SentryMonoBehaviour _monoBehaviour;

        public ANRIntegration(SentryMonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            _watchdog.StartOnce(options.DiagnosticLogger, _monoBehaviour);
            _watchdog.OnApplicationNotResponding += (_, e) => hub.CaptureException(e);
        }
    }

    internal class ANRWatchDog
    {
        private int _detectionTimeoutMs;

        // Note: we don't sleep for the whole detection timeout or we wouldn't capture if the ANR started later.
        private int _sleepIntervalMs;

        private IDiagnosticLogger? _logger;
        private SentryMonoBehaviour _monoBehaviour = null!;
        private int _ticksSinceUiUpdate = 0; // how many _sleepIntervalMs have elapsed since the UI updated last time
        private bool _reported = false; // don't report the same ANR instance multiple times
        private bool _stop = false;
        private Thread _thread = null!;
        internal event EventHandler<ApplicationNotResponding> OnApplicationNotResponding = delegate { };

        internal ANRWatchDog(int detectionTimeoutMilliseconds = 5000)
        {
            _detectionTimeoutMs = detectionTimeoutMilliseconds;
            _sleepIntervalMs = Math.Max(1, _detectionTimeoutMs / 5);
        }

        internal void StartOnce(IDiagnosticLogger? logger, SentryMonoBehaviour monoBehaviour)
        {
            // Start the thread, if not yet running.
            lock (OnApplicationNotResponding)
            {
                if (_thread is null)
                {
                    _monoBehaviour = monoBehaviour;
                    _logger = logger;
                    _thread = new Thread(Run)
                    {
                        Name = "Sentry-ANR-WatchDog",
                        IsBackground = true, // do not block on app shutdown
                        Priority = System.Threading.ThreadPriority.BelowNormal,
                    };
                    _thread.Start();

                    // Stop the thread when the app is being shut down.
                    _monoBehaviour.Application.Quitting += () => Stop();

                    // Update the UI status periodically by running a couroutine on the UI thread
                    _monoBehaviour.StartCoroutine(UpdateUiStatus());
                }
            }
        }

        internal void Stop(bool joinThread = false)
        {
            _stop = true;
            if (joinThread)
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
                    // sleep for a bit
                    Thread.Sleep(_sleepIntervalMs);

                    // time to check
                    ticksSinceUiUpdate = _ticksSinceUiUpdate;
                    // _logger?.LogDebug("ANR status: {0} failed checks since last UI update", ticksSinceUiUpdate);
                    if (ticksSinceUiUpdate >= reportTreshold && !_reported)
                    {
                        var message = $"Application not responding for at least {ticksSinceUiUpdate * _sleepIntervalMs} ms.";
                        _logger?.LogInfo("Detected an ANR event: {0}", message);
                        OnApplicationNotResponding?.Invoke(this, new ApplicationNotResponding(message));
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

    internal class ApplicationNotResponding : Exception
    {
        internal ApplicationNotResponding() : base() { }
        internal ApplicationNotResponding(string message) : base(message) { }
        internal ApplicationNotResponding(string message, Exception innerException) : base(message, innerException) { }
    }
}
