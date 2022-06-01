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
        private int _checkIntervalMilliseconds;

        // Note: we don't sleep for the check interval duration to prevent the thread from blocking shutdown.
        // Note 2: if this is higher than _checkIntervalMilliseconds, the lower value is used.
        private const int sleepInterval = 1000;

        private IDiagnosticLogger? _logger;
        private SentryMonoBehaviour _monoBehaviour = null!;
        private bool _uiIsResponsive = false;
        private bool _reported = false; // don't report the same ANR instance multiple times
        private bool _stop = false;
        private Thread _thread = null!;
        internal event EventHandler<ApplicationNotResponding> OnApplicationNotResponding = delegate { };

        internal ANRWatchDog(int checkIntervalMilliseconds = 5000)
        {
            _checkIntervalMilliseconds = checkIntervalMilliseconds;
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
                        Name = "Sentry-ANR-WatchDog"
                    };
                    _thread.Start();

                    // Stop the thread when the app is being shut down.
                    _monoBehaviour.Application.Quitting += () =>
                    {
                        _stop = true;
                        _thread.Join();
                    };

                    // Update the UI status periodically by running a couroutine on the UI thread
                    _monoBehaviour.StartCoroutine(UpdateUiStatus());
                }
            }
        }

        private IEnumerator UpdateUiStatus()
        {
            var waitForSeconds = new WaitForSeconds((float)Math.Min(sleepInterval, _checkIntervalMilliseconds / 2) / 1000);

            yield return waitForSeconds;
            while (!_stop)
            {
                _uiIsResponsive = true;
                _reported = false;
                yield return waitForSeconds;
            }
        }

        private void Run()
        {
            try
            {
                long msUntilCheck; // avoiding allocs in the loop
                var watch = Stopwatch.StartNew();

                while (!_stop)
                {
                    msUntilCheck = _checkIntervalMilliseconds - watch.ElapsedMilliseconds;
                    if (msUntilCheck > 0)
                    {
                        // sleep for a bit
                        Thread.Sleep(msUntilCheck < sleepInterval ? (int)msUntilCheck : sleepInterval);
                    }
                    else
                    {
                        // time to check
                        if (!_uiIsResponsive && !_reported)
                        {
                            var message = $"Application not responding for at least {_checkIntervalMilliseconds} ms.";
                            _logger?.LogInfo("Detected an ANR event: {0}", message);
                            OnApplicationNotResponding?.Invoke(this, new ApplicationNotResponding(message));
                            _reported = true;
                        }

                        _uiIsResponsive = false;
                        watch.Restart();
                    }
                }
            }
            catch (ThreadAbortException e)
            {
                _logger?.Log(SentryLevel.Warning, "Exception in the ANR watchdog.", e);
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
