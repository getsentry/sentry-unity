using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity.Integrations;

internal sealed class UnityLogHandlerIntegration : ISdkIntegration, ILogHandler
{
    internal readonly ErrorTimeDebounce ErrorTimeDebounce;
    internal readonly LogTimeDebounce LogTimeDebounce;
    internal readonly WarningTimeDebounce WarningTimeDebounce;

    private readonly IApplication _application;

    private IHub? _hub;
    private SentryUnityOptions? _sentryOptions;

    private ILogHandler _unityLogHandler = null!; // Set during register

    public UnityLogHandlerIntegration(SentryUnityOptions options, IApplication? application = null)
    {
            _application = application ?? ApplicationAdapter.Instance;

            LogTimeDebounce = new LogTimeDebounce(options.DebounceTimeLog);
            WarningTimeDebounce = new WarningTimeDebounce(options.DebounceTimeWarning);
            ErrorTimeDebounce = new ErrorTimeDebounce(options.DebounceTimeError);
        }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
            _hub = hub;
            _sentryOptions = sentryOptions as SentryUnityOptions;
            if (_sentryOptions is null)
            {
                return;
            }

            // If called twice (i.e. init with the same options object) the integration will reference itself as the
            // original handler loghandler and endlessly forward to itself
            if (Debug.unityLogger.logHandler == this)
            {
                _sentryOptions.DiagnosticLogger?.LogWarning("UnityLogHandlerIntegration has already been registered.");
                return;
            }

            _unityLogHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = this;

            _application.Quitting += OnQuitting;
        }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
            try
            {
                CaptureException(exception, context);
            }
            finally
            {
                // Always pass the exception back to Unity
                _unityLogHandler.LogException(exception, context);
            }
        }

    internal void CaptureException(Exception exception, UnityEngine.Object? context)
    {
            if (_hub?.IsEnabled is not true)
            {
                return;
            }

            // TODO: Capture the context (i.e. grab the name if != null and set it as context)

            // NOTE: This might not be entirely true, as a user could as well call `Debug.LogException`
            // and expect a handled exception but it is not possible for us to differentiate
            // https://docs.sentry.io/platforms/unity/troubleshooting/#unhandled-exceptions---debuglogexception
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "Unity.LogException";
            _ = _hub.CaptureException(exception);

            if (_sentryOptions?.AddBreadcrumbsForLogType[LogType.Exception] is true)
            {
                // So the next event includes this error as a breadcrumb
                _hub.AddBreadcrumb(message: $"{exception.GetType()}: {exception.Message}", category: "unity.logger", level: BreadcrumbLevel.Error);
            }
        }

    public void LogFormat(LogType logType, UnityEngine.Object? context, string format, params object[] args)
    {
            try
            {
                CaptureLogFormat(logType, context, format, args);
            }
            finally
            {
                // Always pass the log back to Unity
                _unityLogHandler.LogFormat(logType, context, format, args);
            }
        }

    internal void CaptureLogFormat(LogType logType, UnityEngine.Object? context, string format, params object[] args)
    {
            if (_hub?.IsEnabled is not true)
            {
                return;
            }

            // The SDK sets "Sentry" as tag when logging and we're not capturing SDK internal logs. Expected format: "{0}: {1}"
            if (args.Length > 1 && "Sentry".Equals(args[0])) // Checking it this way around because `args[0]` could be null
            {
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

            var logMessage = args.Length == 0 ? format : string.Format(format, args);

            if (logType is LogType.Error or LogType.Assert)
            {
                // TODO: Capture the context (i.e. grab the name if != null and set it as context)
                _hub.CaptureMessage(logMessage, ToEventTagType(logType));
            }

            if (_sentryOptions?.AddBreadcrumbsForLogType[logType] is true)
            {
                // So the next event includes this as a breadcrumb
                _hub.AddBreadcrumb(message: logMessage, category: "unity.logger", level: ToBreadcrumbLevel(logType));
            }
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
            if (_sentryOptions?.AutoSessionTracking is true)
            {
                _hub?.PauseSession();
            }
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