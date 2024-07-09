using System;
using Sentry.Extensibility;
using UnityEngine;
using static System.String;

namespace Sentry.Unity;

public class UnityLogger : IDiagnosticLogger
{
    public const string LogTag = "Sentry";

    private readonly SentryOptions _sentryOptions;
    private readonly ILogger _logger;

    public bool IsEnabled(SentryLevel level) => level >= _sentryOptions.DiagnosticLevel;

    public UnityLogger(SentryUnityOptions sentryUnityOptions) : this(sentryUnityOptions, null)
    { }

    internal UnityLogger(SentryOptions sentryOptions, ILogger? logger = null)
    {
        _sentryOptions = sentryOptions;
        _logger = logger ?? Debug.unityLogger;
    }

    public void Log(SentryLevel logLevel, string? message, Exception? exception = null, params object?[] args)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        _logger.Log(GetUnityLogType(logLevel), LogTag, $"({logLevel.ToString()}) {Format(message, args)} {exception}");
    }

    internal static LogType GetUnityLogType(SentryLevel logLevel)
    {
        return logLevel switch
        {
            SentryLevel.Debug or SentryLevel.Info => LogType.Log,
            SentryLevel.Warning => LogType.Warning,
            SentryLevel.Error or SentryLevel.Fatal => LogType.Error,
            _ => LogType.Log
        };
    }

    public override string ToString() => nameof(UnityLogger);
}
