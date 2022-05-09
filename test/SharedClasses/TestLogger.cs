using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using Sentry.Extensibility;

namespace Sentry.Unity.Tests.SharedClasses
{
    internal sealed class TestLogger : IDiagnosticLogger
    {
        internal readonly ConcurrentBag<(SentryLevel logLevel, string message, Exception? exception)> Logs = new();

        public bool IsEnabled(SentryLevel level) => true;

        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            var log = (logLevel, string.Format(message, args), exception);
            Logs.Add(log);
        }
    }
}
