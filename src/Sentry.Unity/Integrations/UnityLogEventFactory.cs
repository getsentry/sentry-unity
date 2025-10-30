using System;
using System.Collections.Generic;
using System.Threading;
using Sentry.Protocol;

namespace Sentry.Unity.Integrations;

/// <summary>
/// Factory for creating SentryEvent objects from Unity log messages and stacktraces
/// </summary>
internal static class UnityLogEventFactory
{
    /// <summary>
    /// Creates a message event with stacktrace attached via threads (for Debug.LogError)
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="stackTrace">The Unity stacktrace string</param>
    /// <param name="level">The Sentry event level</param>
    /// <param name="options">Sentry Unity options</param>
    /// <returns>A SentryEvent with the message and stacktrace as threads</returns>
    public static SentryEvent CreateMessageEvent(
        string message,
        string stackTrace,
        SentryLevel level,
        SentryUnityOptions options)
    {
        var frames = UnityStackTraceParser.Parse(stackTrace, options);
        frames.Reverse();

        var thread = CreateThreadFromStackTrace(frames);

        return new SentryEvent
        {
            Message = message,
            Level = level,
            SentryThreads = [thread]
        };
    }

    /// <summary>
    /// Creates an exception event from Unity log data (for exceptions on WebGL)
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="stackTrace">The Unity stacktrace string</param>
    /// <param name="options">Sentry Unity options</param>
    /// <returns>A SentryEvent with a synthetic exception</returns>
    public static SentryEvent CreateExceptionEvent(
        string message,
        string stackTrace,
        SentryUnityOptions options)
    {
        var frames = UnityStackTraceParser.Parse(stackTrace, options);
        frames.Reverse();

        var sentryException = CreateUnityLogException(message, frames);

        return new SentryEvent(new Exception(message))
        {
            SentryExceptions = [sentryException],
            Level = SentryLevel.Error
        };
    }

    private static SentryThread CreateThreadFromStackTrace(List<SentryStackFrame> frames)
    {
        var currentThread = Thread.CurrentThread;
        return new SentryThread
        {
            Crashed = false,
            Current = true,
            Name = currentThread.Name,
            Id = currentThread.ManagedThreadId,
            Stacktrace = new SentryStackTrace { Frames = frames }
        };
    }

    private static SentryException CreateUnityLogException(
        string message,
        List<SentryStackFrame> frames,
        string exceptionType = "LogError")
    {
        return new SentryException
        {
            Stacktrace = new SentryStackTrace { Frames = frames },
            Value = message,
            Type = exceptionType,
            Mechanism = new Mechanism
            {
                Handled = true,
                Type = "unity.log"
            }
        };
    }
}
