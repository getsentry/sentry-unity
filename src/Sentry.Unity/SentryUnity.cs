using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    public sealed class SentryUnity : IDisposable
    {
        private static SentryUnity? Instance;

        internal readonly ErrorTimeDebounce ErrorTimeDebounce = new(TimeSpan.FromSeconds(1));
        internal readonly LogTimeDebounce LogTimeDebounce = new(TimeSpan.FromSeconds(1));
        internal readonly WarningTimeDebounce WarningTimeDebounce = new(TimeSpan.FromSeconds(1));

        internal UnitySentryOptions? Options { get; private set; }

        internal IEventCapture EventCapture { get; set; } = new EventCapture();

        public static SentryUnity Init(UnitySentryOptions unitySentryOptions, Action<SentryUnity>? unityConfigure = null)
        {
            // Force dispose if new Init
            if (Instance != null)
            {
                // TODO: discuss
                // using the logger of a new instance
                unitySentryOptions.Logger?.Log(SentryLevel.Warning, "RE-INIT");

                Instance.Dispose();
            }

            SentrySdk.Init(unitySentryOptions.ToSentryOptions());

            Instance = new SentryUnity { Options = unitySentryOptions }
                .WithApplicationEvents()
                .FinalizeSetup(unitySentryOptions.Logger);
            unityConfigure?.Invoke(Instance);
            return Instance;
        }

        public static SentryUnity Init(
            Action<UnitySentryOptions> unitySentryOptionsConfigure,
            Action<SentryUnity>? unityConfigure = null)
        {
            var unitySentryOptions = new UnitySentryOptions();
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            return Init(unitySentryOptions, unityConfigure);
        }

        private SentryUnity WithApplicationEvents()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            Application.quitting += OnQuitting;

            return this;
        }

        private SentryUnity FinalizeSetup(IDiagnosticLogger? diagnosticLogger)
        {
            var data = SceneManager.GetActiveScene().name is { } name
                ? new Dictionary<string, string> {{"scene", name}}
                : null;

            SentrySdk.AddBreadcrumb("BeforeSceneLoad", data: data);

            diagnosticLogger?.Log(SentryLevel.Debug, "Complete Sentry SDK initialization.");

            return this;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            var debounced = type switch
            {
                LogType.Error or LogType.Exception or LogType.Assert => ErrorTimeDebounce.Debounced(),
                LogType.Log => LogTimeDebounce.Debounced(),
                LogType.Warning => WarningTimeDebounce.Debounced(),
                _ => true
            };
            if (!debounced)
            {
                return;
            }

            // TODO: to check against 'MinBreadcrumbLevel'
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                // TODO: MinBreadcrumbLevel
                // options.MinBreadcrumbLevel
                SentrySdk.AddBreadcrumb(condition, level: ToBreadcrumbLevel(type));
                return;
            }

            var sentryEvent = new SentryEvent(new UnityLogException(condition, stackTrace));
            sentryEvent.SetTag("log.type", ToEventTagType(type));
            _ = EventCapture?.Capture(sentryEvent);
            SentrySdk.AddBreadcrumb(condition, level: ToBreadcrumbLevel(type));

            static string ToEventTagType(LogType logType)
                => logType switch
                {
                    LogType.Assert => "assert",
                    LogType.Error => "error",
                    LogType.Exception => "exception",
                    LogType.Log => "log",
                    LogType.Warning => "warning",
                    _ => "unknown"
                };

            static BreadcrumbLevel ToBreadcrumbLevel(LogType logType)
                => logType switch
                {
                    LogType.Assert => BreadcrumbLevel.Error,
                    LogType.Error => BreadcrumbLevel.Error,
                    LogType.Exception => BreadcrumbLevel.Error,
                    LogType.Log => BreadcrumbLevel.Info,
                    LogType.Warning => BreadcrumbLevel.Warning,
                    _ => BreadcrumbLevel.Info
                };
        }

        private void OnQuitting()
        {
            // Note: iOS applications are usually suspended and do not quit. You should tick "Exit on Suspend" in Player settings for iOS builds to cause the game to quit and not suspend, otherwise you may not see this call.
            //   If "Exit on Suspend" is not ticked then you will see calls to OnApplicationPause instead.
            // Note: On Windows Store Apps and Windows Phone 8.1 there is no application quit event. Consider using OnApplicationFocus event when focusStatus equals false.
            // Note: On WebGL it is not possible to implement OnApplicationQuit due to nature of the browser tabs closing.
            Application.logMessageReceived -= OnLogMessageReceived;
            SentrySdk.Close();
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.quitting -= OnQuitting;
            SentrySdk.Close();
        }
    }
}
