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

        // A diagnostic logger must never take its caller down with it. Callers routinely log from
        // inside a `catch` that already handled the failure - if logging throws there, a handled
        // error turns into a fatal one. That is not hypothetical: on Nintendo Switch some
        // exceptions throw again while their stack trace is being stringified, which aborted SDK
        // initialization from inside the handler that had already dealt with the original problem.
        string logMessage;
        try
        {
            logMessage = $"({logLevel.ToString()}) {Format(message, args)} {Describe(exception)}";
        }
        catch (Exception e)
        {
            logMessage = $"({logLevel.ToString()}) Failed to format log message: {e.GetType().Name}";
        }

        try
        {
            _logger.Log(GetUnityLogType(logLevel), LogTag, logMessage);
        }
        catch
        {
            // Reporting a logging failure would have to go through the very thing that just failed.
        }
    }

    /// <summary>
    /// Renders an exception without trusting <see cref="Exception.ToString"/>, which walks the
    /// stack trace and can throw on platforms with limited stack trace support.
    /// </summary>
    private static string Describe(Exception? exception)
    {
        if (exception is null)
        {
            return Empty;
        }

        try
        {
            return exception.ToString();
        }
        catch (Exception e)
        {
            // Type and message are cheap to read and usually survive when the stack trace does not.
            try
            {
                return $"{exception.GetType().FullName}: {exception.Message}" +
                       $" (stack trace unavailable: {e.GetType().Name})";
            }
            catch
            {
                return "<exception details unavailable>";
            }
        }
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
