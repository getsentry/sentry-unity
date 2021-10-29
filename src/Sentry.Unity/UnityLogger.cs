using System;
using Sentry.Extensibility;
using UnityEngine;
using static System.String;

namespace Sentry.Unity
{
    internal interface IUnityLoggerInterceptor
    {
        void Intercept(SentryLevel logLevel, string logMessage);
    }

    public class UnityLogger : IDiagnosticLogger
    {
        public const string LogPrefix = "Sentry: ";

        private readonly SentryOptions _sentryOptions;
        private readonly IUnityLoggerInterceptor? _interceptor;

        public bool IsEnabled(SentryLevel level) => level >= _sentryOptions.DiagnosticLevel;

        public UnityLogger(SentryUnityOptions sentryUnityOptions) : this(sentryUnityOptions, null)
        { }

        internal UnityLogger(SentryOptions sentryOptions, IUnityLoggerInterceptor? interceptor = null)
        {
            _sentryOptions = sentryOptions;
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
                var log = $"{LogPrefix}({logLevel}) {Format(message, args)} {exception}";
                _interceptor?.Intercept(logLevel, log);
                return log;
            }
        }

        public override string ToString() => nameof(UnityLogger);
    }
}
