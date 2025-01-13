using System;
using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity.Integrations;

internal class UnityApplicationLoggingIntegration : ISdkIntegration
{
    private readonly IApplication _application;

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

    private void OnQuitting()
    {
        _application.LogMessageReceived -= OnLogMessageReceived;
    }
}
