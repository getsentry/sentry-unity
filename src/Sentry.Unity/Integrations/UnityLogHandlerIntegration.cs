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
    private readonly IApplication _application;
    private IHub? _hub;
    private SentryUnityOptions _options = null!; // Set during register
    private ILogHandler _unityLogHandler = null!; // Set during register
    internal SentryStructuredLogger _structuredLogger = null!; // Set during register

    public UnityLogHandlerIntegration(IApplication? application = null)
    {
        _application = application ?? ApplicationAdapter.Instance;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        _hub = hub;
        // This should never happen, but if it does...
        _options = sentryOptions as SentryUnityOptions ?? throw new InvalidOperationException("Options is not of type 'SentryUnityOptions'.");
        _structuredLogger = Sentry.SentrySdk.Logger;

        // If called twice (i.e. init with the same options object) the integration will reference itself as the
        // original handler loghandler and endlessly forward to itself
        if (Debug.unityLogger.logHandler == this)
        {
            _options.DiagnosticLogger?.LogWarning("UnityLogHandlerIntegration has already been registered.");
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
        exception.Data[Mechanism.HandledKey] = false;
        exception.Data[Mechanism.MechanismKey] = "Unity.LogException";
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
        if (_hub?.IsEnabled is not true)
        {
            return;
        }

        if (args.Length > 1 && args[0] is UnityLogger.LogTag)
        {
            return;
        }

        ProcessStructuredLog(logType, format, args);
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

    private void OnQuitting()
    {
        _options.DiagnosticLogger?.LogInfo("OnQuitting was invoked. Unhooking log callback and pausing session.");

        // Note: iOS applications are usually suspended and do not quit. You should tick "Exit on Suspend" in Player settings for iOS builds to cause the game to quit and not suspend, otherwise you may not see this call.
        //   If "Exit on Suspend" is not ticked then you will see calls to OnApplicationPause instead.
        // Note: On Windows Store Apps and Windows Phone 8.1 there is no application quit event. Consider using OnApplicationFocus event when focusStatus equals false.
        // Note: On WebGL it is not possible to implement OnApplicationQuit due to nature of the browser tabs closing.

        // 'OnQuitting' is invoked even when an uncaught exception happens in the ART. To make sure the .NET
        // SDK checks with the native layer on restart if the previous run crashed (through the CrashedLastRun callback)
        // we'll just pause sessions on shutdown. On restart they can be closed with the right timestamp and as 'exited'.
        if (_options.AutoSessionTracking)
        {
            _hub?.PauseSession();
        }
        _hub?.FlushAsync(_options.ShutdownTimeout).GetAwaiter().GetResult();
    }
}
