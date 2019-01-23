using System;
using Sentry.Extensibility;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity
{
    public class UnityLogger : IDiagnosticLogger
    {
        public bool IsEnabled(SentryLevel level) => true;

        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args) 
            => Debug.Log($"{logLevel} - {string.Format(message, args)} - {exception?.ToString() ?? null}");

        public override string ToString() => nameof(UnityLogger);
    }
}
