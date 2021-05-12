using Sentry.Extensibility;
using System;
using UnityEngine;
using static System.String;

namespace Sentry.Unity
{
    internal interface IUnityLoggerInterceptor
    {
        void Intercept(string logMessage);
    }

    internal class UnityLogger : IDiagnosticLogger
    {
        private readonly SentryLevel _minimalLevel;
        private readonly IUnityLoggerInterceptor? _interceptor;

        public bool IsEnabled(SentryLevel level) => level >= _minimalLevel;

        public UnityLogger(SentryLevel minimalLevel, IUnityLoggerInterceptor? interceptor = null)
        {
            _minimalLevel = minimalLevel;
            _interceptor = interceptor;
        }

        public void Log(SentryLevel logLevel, string? message, Exception? exception = null, params object?[] args)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            switch (logLevel)
            {
                case SentryLevel.Debug or SentryLevel.Info:
                    Debug.Log(GetLog());
                    break;
                case SentryLevel.Warning:
                    Debug.LogWarning(GetLog());
                    break;
                case SentryLevel.Error or SentryLevel.Fatal:
                    Debug.LogError(GetLog());
                    break;
                default:
                    Debug.Log(GetLog());
                    break;
            }

            string GetLog()
            {
                var log = $@"Sentry: {logLevel}
{Format(message, args)}
{exception}";
                _interceptor?.Intercept(log);
                return log;
            }
        }

        public override string ToString() => nameof(UnityLogger);
    }
}
