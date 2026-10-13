using System;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public sealed class UnityLoggerTests
{
    [Test]
    [TestCase(SentryLevel.Debug, LogType.Log)]
    [TestCase(SentryLevel.Info, LogType.Log)]
    [TestCase(SentryLevel.Warning, LogType.Warning)]
    [TestCase(SentryLevel.Error, LogType.Error)]
    [TestCase(SentryLevel.Fatal, LogType.Error)]
    public void GetUnityLogType_LogTypes_Correspond(SentryLevel sentryLevel, LogType expectedLogType)
    {
        Assert.AreEqual(expectedLogType, UnityLogger.GetUnityLogType(sentryLevel));
    }

    [Test]
    [TestCase(SentryLevel.Info, SentryLevel.Debug)]
    [TestCase(SentryLevel.Warning, SentryLevel.Info)]
    [TestCase(SentryLevel.Error, SentryLevel.Warning)]
    [TestCase(SentryLevel.Fatal, SentryLevel.Error)]
    public void Log_LowerLevelThanInitializationLevel_DisablesLogger(SentryLevel initializationLevel, SentryLevel logLevel)
    {
        LogAssert.ignoreFailingMessages = true;

        var testLogger = new UnityTestLogger();
        var logger = new UnityLogger(new SentryOptions { DiagnosticLevel = initializationLevel }, testLogger);

        const string expectedLog = "Some log";

        logger.Log(logLevel, expectedLog);

        Assert.False(logger.IsEnabled(logLevel));
        Assert.IsEmpty(testLogger.Logs);
    }

    [Test]
    public void Log_SetsTag()
    {
        var testLogger = new UnityTestLogger();
        var logger = new UnityLogger(new SentryOptions { DiagnosticLevel = SentryLevel.Debug }, testLogger);

        logger.Log(SentryLevel.Debug, "TestLog");

        Assert.AreEqual(1, testLogger.Logs.Count);
        // The format is: "(logType, tag, message)"
        StringAssert.AreEqualIgnoringCase(UnityLogger.LogTag, testLogger.Logs[0].Item2);
    }

    /// <summary>
    /// Callers log from inside `catch` blocks that already handled the failure.
    /// </summary>
    [Test]
    public void Log_ExceptionThrowsWhileBeingStringified_DoesNotPropagateAndStillLogs()
    {
        var testLogger = new UnityTestLogger();
        var logger = new UnityLogger(new SentryOptions { DiagnosticLevel = SentryLevel.Debug }, testLogger);

        Assert.DoesNotThrow(() =>
            logger.Log(SentryLevel.Error, "Something failed", new ThrowingToStringException()));

        Assert.AreEqual(1, testLogger.Logs.Count);
        var message = testLogger.Logs[0].Item3;
        StringAssert.Contains("Something failed", message);
        StringAssert.Contains(nameof(ThrowingToStringException), message);
    }

    [Test]
    public void Log_MessageAndArgumentsDoNotMatch_DoesNotPropagateAndKeepsTheRawMessage()
    {
        var testLogger = new UnityTestLogger();
        var logger = new UnityLogger(new SentryOptions { DiagnosticLevel = SentryLevel.Debug }, testLogger);

        // More placeholders than arguments - string.Format throws on this.
        Assert.DoesNotThrow(() => logger.Log(SentryLevel.Debug, "{0} {1} {2}", null, "only-one"));

        Assert.AreEqual(1, testLogger.Logs.Count);
        // The unformatted message is still worth more than a bare "formatting failed".
        StringAssert.Contains("{0} {1} {2}", testLogger.Logs[0].Item3);
    }

    private sealed class ThrowingToStringException : Exception
    {
        public ThrowingToStringException()
        { }

        public ThrowingToStringException(string message) : base(message)
        { }

        public ThrowingToStringException(string message, Exception innerException) : base(message, innerException)
        { }

        public override string ToString() => throw new InvalidOperationException("cannot stringify");

        public override string StackTrace => throw new InvalidOperationException("no stack trace here");
    }
}
