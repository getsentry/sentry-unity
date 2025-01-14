using System;
using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity.Integrations;

internal class UnityApplicationLoggingIntegration : ISdkIntegration
{
    private readonly IApplication _application;
    internal ErrorTimeDebounce? ErrorTimeDebounce = null!;
    internal LogTimeDebounce? LogTimeDebounce = null!;
    internal WarningTimeDebounce? WarningTimeDebounce = null!;

    private IHub? _hub;
    private SentryUnityOptions? _options;

    public UnityApplicationLoggingIntegration(IApplication? application = null)
    {
        _application = application ?? ApplicationAdapter.Instance;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        _hub = hub;
        _options = sentryOptions as SentryUnityOptions;
        if (_options is null)
        {
            return;
        }

        LogTimeDebounce = new LogTimeDebounce(_options.DebounceTimeLog);
        WarningTimeDebounce = new WarningTimeDebounce(_options.DebounceTimeWarning);
        ErrorTimeDebounce = new ErrorTimeDebounce(_options.DebounceTimeError);

        _application.LogMessageReceived += OnLogMessageReceived;
        _application.Quitting += OnQuitting;
    }

    private void OnLogMessageReceived(string condition, string stacktrace, LogType logType)
    {
        // LogType.Exception are getting handled by the UnityLogHandlerIntegration
        if (logType is LogType.Exception)
        {
            return;
        }


    }

    internal void CaptureLog(LogType logType, UnityEngine.Object? context, string format, params object[] args)
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

        if (_options?.EnableLogDebouncing is true)
        {
            var debounced = logType switch
            {
                LogType.Error or LogType.Exception or LogType.Assert => ErrorTimeDebounce?.Debounced(),
                LogType.Log => LogTimeDebounce?.Debounced(),
                LogType.Warning => WarningTimeDebounce?.Debounced(),
                _ => true
            };

            if (debounced is not true)
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

        if (_options?.AddBreadcrumbsForLogType[logType] is true)
        {
            // So the next event includes this as a breadcrumb
            _hub.AddBreadcrumb(message: logMessage, category: "unity.logger", level: ToBreadcrumbLevel(logType));
        }
    }


    private void OnQuitting()
    {
        _application.LogMessageReceived -= OnLogMessageReceived;
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
