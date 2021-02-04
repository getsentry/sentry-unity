using Sentry;
using Sentry.Extensibility;
using System;
using UnityEngine;
using static System.String;

internal class UnityLogger : IDiagnosticLogger
{
    private readonly SentryLevel _minimalLevel;
    public bool IsEnabled(SentryLevel level) => level >= _minimalLevel;
    public UnityLogger(SentryLevel minimalLevel) => _minimalLevel = minimalLevel;

    public void Log(SentryLevel logLevel, string? message, Exception? exception = null, params object?[] args)
    {
        if (IsEnabled(logLevel))
        {
            Debug.Log($@"Sentry: {logLevel}
{Format(message, args)}
{exception}");
        }
    }

    public override string ToString() => nameof(UnityLogger);
}
