using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.Tests.SharedClasses;

internal class UnityTestLogger : ILogger
{
    public ILogHandler? logHandler { get; set; }
    public bool logEnabled { get; set; }
    public LogType filterLogType { get; set; }

    public List<(LogType, string?, string)> Logs { get; private set; } = new();

    public void AssertLogContains(SentryLevel sentryLevel, string message) =>
        CollectionAssert.Contains(
            Logs,
            (UnityLogger.GetUnityLogType(sentryLevel), UnityLogger.LogTag, $"({sentryLevel.ToString()}) {message} "));

    private static string GetString(object? message)
    {
        return message switch
        {
            null => "Null",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => message.ToString()
        };
    }

    // The SDK uses this one method exclusively
    public void Log(LogType logType, string tag, object message)
    {
        Logs.Add((logType, tag, GetString(message)));
    }

    public void LogException(Exception exception)
    {
        throw new NotImplementedException();
    }

    public bool IsLogTypeAllowed(LogType logType)
    {
        throw new NotImplementedException();
    }

    public void Log(LogType logType, object message)
    {
        throw new NotImplementedException();
    }

    public void Log(LogType logType, object message, UnityEngine.Object context)
    {
        throw new NotImplementedException();
    }

    public void Log(LogType logType, string tag, object message, UnityEngine.Object context)
    {
        throw new NotImplementedException();
    }

    public void Log(object message)
    {
        throw new NotImplementedException();
    }

    public void Log(string tag, object message)
    {
        throw new NotImplementedException();
    }

    public void Log(string tag, object message, UnityEngine.Object context)
    {
        throw new NotImplementedException();
    }

    public void LogWarning(string tag, object message)
    {
        throw new NotImplementedException();
    }

    public void LogWarning(string tag, object message, UnityEngine.Object context)
    {
        throw new NotImplementedException();
    }

    public void LogError(string tag, object message)
    {
        throw new NotImplementedException();
    }

    public void LogError(string tag, object message, UnityEngine.Object context)
    {
        throw new NotImplementedException();
    }

    public void LogFormat(LogType logType, string format, params object[] args)
    {
        throw new NotImplementedException();
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        throw new NotImplementedException();
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        throw new NotImplementedException();
    }
}