using Sentry.Integrations;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity.Integrations;

/// <summary>
/// Hooks into Unity's `Application.LogMessageReceived` to capture breadcrumbs for Debug log methods
/// and optionally capture LogError events. Does not handle `Debug.LogException` since it lacks the
/// actual exception object needed for IL2CPP processing, except on WebGL where it's treated as a log message.
/// </summary>
internal class UnityApplicationLoggingIntegration : ISdkIntegration
{
    private readonly IApplication _application;
    private readonly bool _captureExceptions;
    private ErrorTimeDebounce? _errorTimeDebounce;
    private LogTimeDebounce? _logTimeDebounce;
    private WarningTimeDebounce? _warningTimeDebounce;

    private IHub? _hub;
    private SentryUnityOptions? _options;

    internal UnityApplicationLoggingIntegration(bool captureExceptions = false, IApplication? application = null)
    {
        _captureExceptions = captureExceptions;
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
        // Unless we're configured to handle them - i.e. WebGL
        if (logType is LogType.Exception && !_captureExceptions)
        {
            return;
        }

        if (_options?.EnableLogDebouncing is true)
        {
            var debounced = logType switch
            {
                LogType.Exception => _errorTimeDebounce?.Debounced(),
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

        if (logType is LogType.Exception)
        {
            var ule = new UnityErrorLogException(message, stacktrace, _options);
            _hub.CaptureException(ule);

            // We don't capture breadcrumbs for exceptions - the .NET SDK handles this
            return;
        }

        if (logType is LogType.Error && _options?.CaptureLogErrorEvents is true)
        {
            if (_options?.AttachStacktrace is true && !string.IsNullOrEmpty(stacktrace))
            {
                var frames = UnityErrorLogException.ParseStackTrace(stacktrace, _options);
                frames.Reverse();

                var thread = new SentryThread
                {
                    Crashed = false,
                    Current = true,
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = frames
                    }
                };

                var sentryEvent = new SentryEvent
                {
                    Message = message,
                    Level = SentryLevel.Error,
                    SentryThreads = [thread]
                };

                _hub.CaptureEvent(sentryEvent);
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
