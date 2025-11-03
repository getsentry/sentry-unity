using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity.Integrations;

/// <summary>
/// Intercepts Unity's log handler to capture `Debug.LogException` calls with actual exception objects.
/// Other log types are handled by ApplicationLoggingIntegration.
/// </summary>
internal sealed class UnityLogHandlerIntegration : ISdkIntegration, ILogHandler
{
    private readonly Func<SentryStructuredLogger>? _loggerFactory;
    private IHub? _hub;
    private SentryUnityOptions _options = null!; // Set during register
    private ILogHandler _unityLogHandler = null!; // Set during register
    private SentryStructuredLogger _structuredLogger = null!; // Set during register

    // For testing: allows injecting a custom logger factory
    internal UnityLogHandlerIntegration(Func<SentryStructuredLogger>? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        _hub = hub;
        // This should never happen, but if it does...
        _options = sentryOptions as SentryUnityOptions ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");
        _structuredLogger = _loggerFactory?.Invoke() ?? _hub.Logger;

        // If called twice (i.e. init with the same options object) the integration will reference itself as the
        // original handler loghandler and endlessly forward to itself
        if (Debug.unityLogger.logHandler == this)
        {
            _options.DiagnosticLogger?.LogWarning("UnityLogHandlerIntegration has already been registered.");
            return;
        }

        _unityLogHandler = Debug.unityLogger.logHandler;
        Debug.unityLogger.logHandler = this;
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        try
        {
            ProcessException(exception, context);
        }
        finally
        {
            // Always pass the exception back to Unity
            _unityLogHandler.LogException(exception, context);
        }
    }

    internal void ProcessException(Exception exception, UnityEngine.Object? context)
    {
        if (_hub?.IsEnabled is not true)
        {
            return;
        }

        // TODO: Capture the context (i.e. grab the name if != null and set it as context)

        // NOTE: This might not be entirely true, as a user could as well call `Debug.LogException`
        // and expect a handled exception but it is not possible for us to differentiate
        // https://docs.sentry.io/platforms/unity/troubleshooting/#unhandled-exceptions---debuglogexception
        exception.SetSentryMechanism("Unity.LogException", handled: false, terminal: false);
        _ = _hub.CaptureException(exception);

        if (_options.Experimental.CaptureStructuredLogsForLogType.TryGetValue(LogType.Exception, out var captureException) && captureException)
        {
            _options.LogDebug("Capturing structured log message of type '{0}'.", LogType.Exception);
            _structuredLogger.LogError(exception.Message);
        }
    }

    public void LogFormat(LogType logType, UnityEngine.Object? context, string format, params object[] args)
    {
        try
        {
            ProcessLog(logType, context, format, args);
        }
        finally
        {
            // Always pass the log back to Unity
            // Capturing of `Debug`, `Warning`, and `Error` happens in the Application Logging Integration.
            // The LogHandler does not have access to the stacktrace information required
            _unityLogHandler.LogFormat(logType, context, format, args);
        }
    }

    private void ProcessLog(LogType logType, UnityEngine.Object? context, string format, params object[] args)
    {
        if (_hub?.IsEnabled is not true || !_options.Experimental.EnableLogs)
        {
            return;
        }

        // We're not capturing the SDK's own logs.
        if (args.Length > 1 && Equals(args[0], UnityLogger.LogTag))
        {
            return;
        }

        // if (IsGettingDebounced(message, stacktrace, logType))
        // {
        //     _options.LogDebug("Log message of type '{0}' is getting debounced.", logType);
        //     return;
        // }

        ProcessStructuredLog(logType, format, args);
    }

    private bool IsGettingDebounced(string message, string stacktrace, LogType logType)
    {
        if (_options.EnableLogDebouncing is false)
        {
            return false;
        }

        // Use the debouncer from options - returns true if allowed, false if blocked
        return !_options.LogDebouncer.Debounced(message, stacktrace, logType);
    }

    private void ProcessStructuredLog(LogType logType, string format, params object[] args)
    {
        if (!_options.Experimental.CaptureStructuredLogsForLogType.TryGetValue(logType, out var captureLog) || !captureLog)
        {
            return;
        }

        _options.LogDebug("Capturing structured log message of type '{0}'.", logType);

        switch (logType)
        {
            case LogType.Log:
                _structuredLogger.LogInfo(format, args);
                break;
            case LogType.Warning:
                _structuredLogger.LogWarning(format, args);
                break;
            case LogType.Assert:
            case LogType.Error:
                _structuredLogger.LogError(format, args);
                break;
        }
    }
}
