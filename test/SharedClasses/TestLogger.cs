using System;
using System.Collections.Concurrent;
using Sentry.Extensibility;
using static System.String;

namespace Sentry.Unity.Tests.SharedClasses;

internal sealed class TestLogger : IDiagnosticLogger
{
    private bool _forwardToUnityLog;

    internal TestLogger(bool forwardToUnityLog = false)
    {
        _forwardToUnityLog = forwardToUnityLog;
    }

    internal readonly ConcurrentBag<(SentryLevel logLevel, string message, Exception? exception)> Logs = new();

    public bool IsEnabled(SentryLevel level) => true;

    public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
    {
        var log = (logLevel, string.Format(message, args), exception);
        Logs.Add(log);

        if (_forwardToUnityLog)
        {
            UnityEngine.Debug.Log($"SentryTestLogger({logLevel}) {Format(message, args)} {exception}");
        }
    }
}