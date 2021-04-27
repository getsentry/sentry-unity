using System;
using System.Collections.Generic;
using Sentry.Integrations;
using Sentry.Unity.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    public sealed class SentryUnity
    {
        public static IDisposable Init(UnitySentryOptions unitySentryOptions)
        {
            // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
            unitySentryOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;

            unitySentryOptions.ConfigureRelease();
            unitySentryOptions.ConfigureEnvironment();
            unitySentryOptions.ConfigureRequestBodyCompressionLevel();
            unitySentryOptions.AddInAppExclude("UnityEngine");
            unitySentryOptions.AddInAppExclude("UnityEditor");
            unitySentryOptions.AddEventProcessor(new UnityEventProcessor());
            unitySentryOptions.AddExceptionProcessor(new UnityEventExceptionProcessor());

            return SentrySdk.Init(unitySentryOptions);
        }

        public static IDisposable Init(Action<UnitySentryOptions> unitySentryOptionsConfigure)
        {
            var unitySentryOptions = new UnitySentryOptions();
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            return Init(unitySentryOptions);
        }
    }

    internal sealed class UnityBeforeSceneLoadIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            var data = SceneManager.GetActiveScene().name is { } name
                ? new Dictionary<string, string> {{"scene", name}}
                : null;

            SentrySdk.AddBreadcrumb("BeforeSceneLoad", data: data);

            options.DiagnosticLogger?.Log(SentryLevel.Debug, "Complete Sentry SDK initialization.");
        }
    }

    internal sealed class UnityApplicationLoggingIntegration : ISdkIntegration
    {
        private readonly IEventCapture _eventCapture;

        internal readonly ErrorTimeDebounce ErrorTimeDebounce = new(TimeSpan.FromSeconds(1));
        internal readonly LogTimeDebounce LogTimeDebounce = new(TimeSpan.FromSeconds(1));
        internal readonly WarningTimeDebounce WarningTimeDebounce = new(TimeSpan.FromSeconds(1));

        public UnityApplicationLoggingIntegration(IEventCapture eventCapture)
        {
            _eventCapture = eventCapture;
        }

        public void Register(IHub _, SentryOptions options)
        {
            Application.logMessageReceived += OnLogMessageReceived;
            Application.quitting += OnQuitting;
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
            _ = _eventCapture?.Capture(sentryEvent);
            SentrySdk.AddBreadcrumb(condition, level: ToBreadcrumbLevel(type));
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

        private static string ToEventTagType(LogType logType)
            => logType switch
            {
                LogType.Assert => "assert",
                LogType.Error => "error",
                LogType.Exception => "exception",
                LogType.Log => "log",
                LogType.Warning => "warning",
                _ => "unknown"
            };

        private static BreadcrumbLevel ToBreadcrumbLevel(LogType logType)
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
}
