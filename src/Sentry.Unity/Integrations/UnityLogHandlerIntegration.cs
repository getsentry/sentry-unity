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
    private IHub? _hub;
    private SentryUnityOptions _options = null!; // Set during register
    private ILogHandler _unityLogHandler = null!; // Set during register

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        _hub = hub;
        // This should never happen, but if it does...
        _options = sentryOptions as SentryUnityOptions ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

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

        // Check throttling - only affects event capture
        if (_options.ErrorEventThrottler is { } throttler && !throttler.ShouldCaptureException(exception))
        {
            _options.LogDebug("Exception event throttled: {0}", exception.GetType().Name);
            return;
        }

        // TODO: Capture the context (i.e. grab the name if != null and set it as context)

        // NOTE: This might not be entirely true, as a user could as well call `Debug.LogException`
        // and expect a handled exception but it is not possible for us to differentiate
        // https://docs.sentry.io/platforms/unity/troubleshooting/#unhandled-exceptions---debuglogexception
        exception.SetSentryMechanism("Unity.LogException", handled: false, terminal: false);
        _ = _hub.CaptureException(exception);
    }

    public void LogFormat(LogType logType, UnityEngine.Object? context, string format, params object[] args)
    {
        // Always pass the log back to Unity
        // Capturing of `Debug`, `Warning`, and `Error` happens in the Application Logging Integration.
        // The LogHandler does not have access to the stacktrace information required
        _unityLogHandler.LogFormat(logType, context, format, args);
    }
}
