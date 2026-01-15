using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity.Integrations;

/// <summary>
/// WebGL-specific exception handler that captures exceptions through Application.LogMessageReceived.
/// Required because UnityLogHandlerIntegration (Debug.unityLogger.logHandler) doesn't work on WebGL.
/// </summary>
internal sealed class UnityWebGLExceptionHandler : ISdkIntegration
{
    private readonly IApplication _application;
    private IHub _hub = null!;
    private SentryUnityOptions _options = null!;

    internal UnityWebGLExceptionHandler(IApplication? application = null)
    {
        _application = application ?? ApplicationAdapter.Instance;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        _hub = hub ?? throw new ArgumentException("Hub is null.");
        _options = sentryOptions as SentryUnityOptions
            ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        _application.LogMessageReceived += OnLogMessageReceived;
        _application.Quitting += OnQuitting;
    }

    internal void OnLogMessageReceived(string message, string stacktrace, LogType logType)
    {
        if (!_hub.IsEnabled)
        {
            return;
        }

        if (logType is not LogType.Exception)
        {
            return;
        }

        // We're not capturing the SDK's own logs
        if (message.StartsWith(UnityLogger.LogTag))
        {
            return;
        }

        // Check throttling - only affects event capture
        if (_options.LogThrottler is { } throttler && !throttler.ShouldCapture(message, stacktrace, logType))
        {
            _options.LogDebug("Exception event throttled.");
            return;
        }

        _options.LogDebug("Capturing exception on WebGL through LogMessageReceived.");
        var evt = UnityLogEventFactory.CreateExceptionEvent(message, stacktrace, false, _options);
        _hub.CaptureEvent(evt);
    }

    private void OnQuitting() => _application.LogMessageReceived -= OnLogMessageReceived;
}
