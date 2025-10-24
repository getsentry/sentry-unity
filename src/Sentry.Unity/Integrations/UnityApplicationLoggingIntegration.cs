using System;
using Sentry.Extensibility;
using Sentry.Integrations;
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

    private ErrorTimeDebounce _errorTimeDebounce = null!;       // Set in Register
    private LogTimeDebounce _logTimeDebounce = null!;           // Set in Register
    private WarningTimeDebounce _warningTimeDebounce = null!;   // Set in Register

    private IHub? _hub;
    private SentryUnityOptions _options = null!;                // Set in Register

    internal UnityApplicationLoggingIntegration(bool captureExceptions = false, IApplication? application = null)
    {
        _captureExceptions = captureExceptions;
        _application = application ?? ApplicationAdapter.Instance;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        _hub = hub;
        // This should never throw
        _options = sentryOptions as SentryUnityOptions ?? throw new InvalidOperationException("Options passed is not of type SentryUnityOptions");

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

        // We're not capturing the SDKs own logs
        if (message.StartsWith(UnityLogger.LogTag))
        {
            return;
        }

        if (IsGettingDebounced(logType))
        {
            _options.LogDebug("Log message of type '{0}' is getting debounced.", logType);
            return;
        }

        ProcessException(message, stacktrace, logType);
        ProcessError(message, stacktrace, logType);
        ProcessBreadcrumbs(message, logType);
    }

    private bool IsGettingDebounced(LogType logType)
    {
        if (_options.EnableLogDebouncing is false)
        {
            return false;
        }

        return logType switch
        {
            LogType.Exception => !_errorTimeDebounce.Debounced(),
            LogType.Error or LogType.Assert => !_errorTimeDebounce.Debounced(),
            LogType.Log => !_logTimeDebounce.Debounced(),
            LogType.Warning => !_warningTimeDebounce.Debounced(),
            _ => true
        };
    }

    private void ProcessException(string message, string stacktrace, LogType logType)
    {
        // LogType.Exception is getting handled by the `UnityLogHandlerIntegration`
        // UNLESS we're configured to handle them - i.e. on WebGL
        if (logType is LogType.Exception && _captureExceptions)
        {
            _options.LogDebug("Exception capture has been enabled. Capturing exception through '{0}'.", nameof(UnityApplicationLoggingIntegration));

            var ule = new UnityErrorLogException(message, stacktrace, _options);
            _hub?.CaptureException(ule);
        }
    }

    private void ProcessError(string message, string stacktrace, LogType logType)
    {
        if (logType is not LogType.Error || !_options.CaptureLogErrorEvents)
        {
            return;
        }

        _options.LogDebug("Error capture for 'Debug.LogError' is enabled. Capturing message.");

        if (_options.AttachStacktrace && !string.IsNullOrEmpty(stacktrace))
        {
            _options.LogDebug("Attaching stacktrace to event.");

            var ule = new UnityErrorLogException(message, stacktrace, _options);
            var sentryEvent = new SentryEvent(ule) { Level = SentryLevel.Error };

            _hub?.CaptureEvent(sentryEvent);
        }
        else
        {
            _hub?.CaptureMessage(message, level: SentryLevel.Error);
        }
    }

    private void ProcessBreadcrumbs(string message, LogType logType)
    {
        if (logType is LogType.Exception)
        {
            // Capturing of breadcrumbs for exceptions happens inside the .NET SDK
            return;
        }

        // Breadcrumb collection on top of structure log capture must be opted in
        if (_options.Experimental is { EnableLogs: true, AttachBreadcrumbsToEvents: false })
        {
            return;
        }

        if (_options.AddBreadcrumbsForLogType.TryGetValue(logType, out var value) && value)
        {
            _options.LogDebug("Adding breadcrumb for log message of type: {0}", logType);
            _hub?.AddBreadcrumb(message: message, category: "unity.logger", level: ToBreadcrumbLevel(logType));
        }
    }

    private void OnQuitting() => _application.LogMessageReceived -= OnLogMessageReceived;

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
