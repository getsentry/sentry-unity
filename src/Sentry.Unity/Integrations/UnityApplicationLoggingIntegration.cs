using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity.Integrations;

internal class UnityApplicationLoggingIntegration : ISdkIntegration
{
    private readonly IApplication _application;
    private ErrorTimeDebounce? _errorTimeDebounce;
    private LogTimeDebounce? _logTimeDebounce;
    private WarningTimeDebounce? _warningTimeDebounce;

    private IHub? _hub;
    private SentryUnityOptions? _options;

    internal UnityApplicationLoggingIntegration(IApplication? application = null)
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

        _logTimeDebounce = new LogTimeDebounce(_options.DebounceTimeLog);
        _warningTimeDebounce = new WarningTimeDebounce(_options.DebounceTimeWarning);
        _errorTimeDebounce = new ErrorTimeDebounce(_options.DebounceTimeError);

        _application.LogMessageReceived += OnLogMessageReceived;
        _application.Quitting += OnQuitting;
    }

    internal void OnLogMessageReceived(string message, string stacktrace, LogType logType)
    {
        if (_hub is null)
        {
            return;
        }

        // We're not capturing or creating breadcrumbs from SDK logs
        if (message.StartsWith(UnityLogger.LogTag))
        {
            return;
        }

        // LogType.Exception are getting handled by the UnityLogHandlerIntegration
        if (logType is LogType.Exception)
        {
            return;
        }

        if (_options?.EnableLogDebouncing is true)
        {
            var debounced = logType switch
            {
                LogType.Error or LogType.Assert => _errorTimeDebounce?.Debounced(),
                LogType.Log => _logTimeDebounce?.Debounced(),
                LogType.Warning => _warningTimeDebounce?.Debounced(),
                _ => true
            };

            if (debounced is not true)
            {
                return;
            }
        }

        if (logType is LogType.Error && _options?.CaptureLogErrorEvents is true)
        {
            if (_options?.AttachStacktrace is true && !string.IsNullOrEmpty(stacktrace))
            {
                var ule = new UnityErrorLogException(message, stacktrace, _options);
                var evt = new SentryEvent(ule) { Level = SentryLevel.Error };

                _hub.CaptureEvent(evt);
            }
            else
            {
                _hub.CaptureMessage(message, level: SentryLevel.Error);
            }
        }

        // Capture so the next event includes this error as breadcrumb
        if (_options?.AddBreadcrumbsForLogType[logType] is true)
        {
            _hub.AddBreadcrumb(message: message, category: "unity.logger", level: ToBreadcrumbLevel(logType));
        }
    }

    private void OnQuitting()
    {
        _application.LogMessageReceived -= OnLogMessageReceived;
    }

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
