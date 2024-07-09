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
}