using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sentry.Extensibility;

namespace Sentry.Unity.Editor.Tests
{
    internal class TestUnityLoggerInterceptor : IUnityLoggerInterceptor, IDiagnosticLogger
    {
        public List<(SentryLevel, string)> Messages { get; private set; } = new();

        public void Intercept(SentryLevel logLevel, string logMessage) => Messages.Add((logLevel, logMessage));
        public bool IsEnabled(SentryLevel level) => true;

        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
            => Messages.Add((logLevel, message));

        public void AssertLogContains(SentryLevel sentryLevel, string message)
            => CollectionAssert.Contains(Messages, (sentryLevel, $"Sentry: ({sentryLevel.ToString()}) {message} "));
    }
}
