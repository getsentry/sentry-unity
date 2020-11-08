using Sentry;
using Sentry.Extensibility;
using System;
using UnityEngine;

internal class UnityLogger : IDiagnosticLogger
{
    public bool IsEnabled(SentryLevel level) => true;

    public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
        => Debug.Log($"{logLevel} - {string.Format(message, args)} - {exception}");

    public override string ToString() => nameof(UnityLogger);
}
