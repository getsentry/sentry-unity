using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity.Integrations
{
    internal sealed class UnityLogHandlerIntegration : ISdkIntegration, ILogHandler
    {
        internal readonly ErrorTimeDebounce ErrorTimeDebounce = new(TimeSpan.FromSeconds(1));
        internal readonly LogTimeDebounce LogTimeDebounce = new(TimeSpan.FromSeconds(1));
        internal readonly WarningTimeDebounce WarningTimeDebounce = new(TimeSpan.FromSeconds(1));

        private readonly IApplication _application;

        private IHub? _hub;
        private SentryUnityOptions? _sentryOptions;

        private ILogHandler _unityLogHandler = null!; // Set during register

        public UnityLogHandlerIntegration(IApplication? application = null)
        {
            _application = application ?? ApplicationAdapter.Instance;
        }

        public void Register(IHub hub, SentryOptions sentryOptions)
        {
            _hub = hub;
            _sentryOptions = sentryOptions as SentryUnityOptions;

            _unityLogHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = this;

            _application.Quitting += OnQuitting;
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            CaptureException(exception, context);
            _unityLogHandler.LogException(exception, context);
        }

        internal void CaptureException(Exception exception, UnityEngine.Object? context)
        {
            if (_hub?.IsEnabled is not true)
            {
                return;
            }

            // TODO: Capture the context (i.e. grab the name if != null)

            // NOTE: This might not be entirely true, as a user could as well call `Debug.LogException`
            // and expect a handled exception but it is not possible for us to differentiate
            // https://docs.sentry.io/platforms/unity/troubleshooting/#unhandled-exceptions---debuglogexception
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "Unity.LogException";
            _ = _hub.CaptureException(exception);

            // So the next event includes this error as a breadcrumb
            _hub.AddBreadcrumb(message: $"{exception.GetType()}: {exception.Message}", category: "unity.logger", level: BreadcrumbLevel.Error);
        }

        public void LogFormat(LogType logType, UnityEngine.Object? context, string format, params object[] args)
        {
            CaptureLogFormat(logType, context, format, args);
            _unityLogHandler.LogFormat(logType, context, format, args);
        }

        internal void CaptureLogFormat(LogType logType, UnityEngine.Object? context, string format, params object[] args)
        {
            if (_hub?.IsEnabled is not true)
            {
                return;
            }

            // TODO: Figure out if format {0} and args.length == 1 is guaranteed?
            // TODO: Capture the context (i.e. grab the name if != null)

            if (!format.Equals("{0}") || args.Length is > 1 or <= 0)
            {
                return;
            }

            if (args[0] is not string logMessage)
            {
                return;
            }

            // We're not capturing SDK internal logs
            if (logMessage.StartsWith(UnityLogger.LogPrefix, StringComparison.Ordinal))
            {
                // TODO: Maybe color Sentry internal logs (highlight 'Sentry'?)
                return;
            }

            if (_sentryOptions?.EnableLogDebouncing is true)
            {
                var debounced = logType switch
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
            }

            // TODO: to check against 'MinBreadcrumbLevel'
            if (logType is LogType.Error or LogType.Assert)
            {
                _hub.CaptureMessage(logMessage, ToEventTagType(logType));
            }

            _hub.AddBreadcrumb(message: logMessage, category: "unity.logger", level: ToBreadcrumbLevel(logType));
        }

        private void OnQuitting()
        {
            _sentryOptions?.DiagnosticLogger?.LogInfo("OnQuitting was invoked. Unhooking log callback and pausing session.");

            // Note: iOS applications are usually suspended and do not quit. You should tick "Exit on Suspend" in Player settings for iOS builds to cause the game to quit and not suspend, otherwise you may not see this call.
            //   If "Exit on Suspend" is not ticked then you will see calls to OnApplicationPause instead.
            // Note: On Windows Store Apps and Windows Phone 8.1 there is no application quit event. Consider using OnApplicationFocus event when focusStatus equals false.
            // Note: On WebGL it is not possible to implement OnApplicationQuit due to nature of the browser tabs closing.

            // 'OnQuitting' is invoked even when an uncaught exception happens in the ART. To make sure the .NET
            // SDK checks with the native layer on restart if the previous run crashed (through the CrashedLastRun callback)
            // we'll just pause sessions on shutdown. On restart they can be closed with the right timestamp and as 'exited'.
            _hub?.PauseSession();
            _hub?.FlushAsync(_sentryOptions?.ShutdownTimeout ?? TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
        }

        private static SentryLevel ToEventTagType(LogType logType)
            => logType switch
            {
                LogType.Assert => SentryLevel.Error,
                LogType.Error => SentryLevel.Error,
                LogType.Exception => SentryLevel.Error,
                LogType.Log => SentryLevel.Info,
                LogType.Warning => SentryLevel.Warning,
                _ => SentryLevel.Fatal
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
