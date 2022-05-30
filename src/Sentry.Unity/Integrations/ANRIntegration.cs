using System;
using System.Diagnostics;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity
{
    internal class ANRIntegration : ISdkIntegration
    {
        private static ANRWatchDog _watchdog = new ANRWatchDog();

        public void Register(IHub hub, SentryOptions options)
        {
            _watchdog.Attach(hub, options.DiagnosticLogger);
        }
    }

    internal class ANRWatchDog
    {
        private const int checkIntervalMilliseconds = 5000;

        // Note: we don't sleep for the check interval duration to prevent the thread from blocking shutdown.
        private const int sleepInterval = 1000;

        // TODO stop on app shutdown
        private CancellationTokenSource _cancel = new CancellationTokenSource();
        private IDiagnosticLogger? _logger;
        private bool _uiIsResponsive = false;
        private Thread _thread = null!;
        private event Action _event = () => { };

        public void Attach(IHub hub, IDiagnosticLogger? logger)
        {
            // Start the thread, if not yet running
            lock (_cancel)
            {
                _logger ??= logger;
                _thread ??= new Thread(Run)
                {
                    Name = "Sentry-ANR-WatchDog"
                };
            }

            _event += () =>
            {
                // TODO trigger even on `hub`
            };
        }

        private void CheckUiThread()
        {
            // TODO implement
        }

        private void Run()
        {
            try
            {
                if (!_uiIsResponsive)
                {
                    CheckUiThread();
                }

                long msUntilCheck; // avoiding allocs in the loop
                var watch = new Stopwatch();
                watch.Start();

                while (!_cancel.IsCancellationRequested)
                {
                    msUntilCheck = checkIntervalMilliseconds - watch.ElapsedMilliseconds;
                    if (msUntilCheck > 0)
                    {
                        // sleep for a bit
                        Thread.Sleep(msUntilCheck < sleepInterval ? (int)msUntilCheck : sleepInterval);
                    }
                    else
                    {
                        // time to check
                        if (!_uiIsResponsive)
                        {
                            _event?.Invoke();
                        }

                        _uiIsResponsive = false;
                        CheckUiThread();
                        watch.Reset();
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Log(SentryLevel.Warning, "Exception in the ANR watchdog.", e);
            }
        }
    }
}
