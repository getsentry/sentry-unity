using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Protocol;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public sealed class UnityLogHandlerIntegrationTests
{
    private class Fixture
    {
        public TestHub Hub { get; set; } = null!;
        public SentryUnityOptions SentryOptions { get; set; } = null!;
        public TestStructuredLogger? StructuredLogger { get; set; }

        public UnityLogHandlerIntegration GetSut()
        {
            var integration = StructuredLogger != null
                ? new UnityLogHandlerIntegration(() => StructuredLogger)
                : new UnityLogHandlerIntegration();
            integration.Register(Hub, SentryOptions);
            return integration;
        }
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture
        {
            Hub = new TestHub(),
            SentryOptions = new SentryUnityOptions()
        };
    }

    [Test]
    public void CaptureException_ExceptionCapturedAndMechanismSet()
    {
        var sut = _fixture.GetSut();
        var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

        sut.ProcessException(new Exception(message), null);

        Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);

        var capturedEvent = _fixture.Hub.CapturedEvents.Single();
        Assert.NotNull(capturedEvent);

        Assert.NotNull(capturedEvent.Exception);
        Assert.AreEqual(message, capturedEvent.Exception!.Message);

        Assert.IsTrue(capturedEvent.Exception!.Data.Contains(Mechanism.HandledKey));
        Assert.IsFalse((bool)capturedEvent.Exception!.Data[Mechanism.HandledKey]);

        Assert.IsTrue(capturedEvent.Exception!.Data.Contains(Mechanism.MechanismKey));
        Assert.AreEqual("Unity.LogException", (string)capturedEvent.Exception!.Data[Mechanism.MechanismKey]);
    }

    [Test]
    public void Register_RegisteredASecondTime_LogsWarningAndReturns()
    {
        var testLogger = new TestLogger();
        _fixture.SentryOptions.DiagnosticLogger = testLogger;
        _fixture.SentryOptions.Debug = true;
        var sut = _fixture.GetSut();

        // Edge-case of initializing the SDK twice with the same options object.
        sut.Register(_fixture.Hub, _fixture.SentryOptions);

        Assert.IsTrue(testLogger.Logs.Any(log =>
            log.logLevel == SentryLevel.Warning &&
            log.message.Contains("UnityLogHandlerIntegration has already been registered.")));
    }

    [Test]
    public void ProcessException_ExperimentalCaptureEnabled_CapturesStructuredLog()
    {
        _fixture.SentryOptions.Experimental.EnableLogs = true;
        _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Exception] = true;
        _fixture.StructuredLogger = new TestStructuredLogger();
        var sut = _fixture.GetSut();
        var message = TestContext.CurrentContext.Test.Name;

        sut.ProcessException(new Exception(message), null);

        Assert.AreEqual(1, _fixture.StructuredLogger.LogCalls.Count);
        var logCall = _fixture.StructuredLogger.LogCalls.Single();
        Assert.AreEqual("Error", logCall.level);
        Assert.AreEqual(message, logCall.message);
    }

    [Test]
    public void ProcessException_ExperimentalCaptureDisabled_DoesNotCaptureStructuredLog()
    {
        _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Exception] = false;
        _fixture.StructuredLogger = new TestStructuredLogger();
        var sut = _fixture.GetSut();
        var message = TestContext.CurrentContext.Test.Name;

        sut.ProcessException(new Exception(message), null);

        Assert.AreEqual(0, _fixture.StructuredLogger.LogCalls.Count);
    }

    [Test]
    public void LogFormat_WithSentryLogTag_DoesNotCaptureStructuredLog()
    {
        _fixture.SentryOptions.Experimental.EnableLogs = true;
        _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Error] = true;
        _fixture.StructuredLogger = new TestStructuredLogger();
        var sut = _fixture.GetSut();

        const string? format = "{0}: {1}";
        const string? message = "Test message";
        LogAssert.Expect(LogType.Error, string.Format(format, UnityLogger.LogTag, message));

        sut.LogFormat(LogType.Error, null, format, UnityLogger.LogTag, message);

        Assert.AreEqual(0, _fixture.StructuredLogger.LogCalls.Count);
    }

    [Test]
    public void LogFormat_WithEnableLogsFalse_DoesNotCaptureStructuredLog()
    {
        _fixture.SentryOptions.Experimental.EnableLogs = false;
        _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Error] = true;
        _fixture.StructuredLogger = new TestStructuredLogger();
        var sut = _fixture.GetSut();
        var message = TestContext.CurrentContext.Test.Name;

        LogAssert.Expect(LogType.Error, message);

        sut.LogFormat(LogType.Error, null, message);

        Assert.AreEqual(0, _fixture.StructuredLogger.LogCalls.Count);
    }

    [Test]
    [TestCase(LogType.Log, "Info", true)]
    [TestCase(LogType.Log, "Info", false)]
    [TestCase(LogType.Warning, "Warning", true)]
    [TestCase(LogType.Warning, "Warning", false)]
    [TestCase(LogType.Error, "Error", true)]
    [TestCase(LogType.Error, "Error", false)]
    [TestCase(LogType.Assert, "Error", true)]
    [TestCase(LogType.Assert, "Error", false)]
    public void LogFormat_WithExperimentalFlag_CapturesStructuredLogWhenEnabled(LogType logType, string expectedLevel, bool captureEnabled)
    {
        _fixture.SentryOptions.Experimental.EnableLogs = true;
        _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[logType] = captureEnabled;
        _fixture.StructuredLogger = new TestStructuredLogger();
        var sut = _fixture.GetSut();
        var message = TestContext.CurrentContext.Test.Name;

        LogAssert.Expect(logType, message);

        sut.LogFormat(logType, null, message);

        if (captureEnabled)
        {
            Assert.AreEqual(1, _fixture.StructuredLogger.LogCalls.Count);
            var logCall = _fixture.StructuredLogger.LogCalls.Single();
            Assert.AreEqual(expectedLevel, logCall.level);
            Assert.AreEqual(message, logCall.message);
        }
        else
        {
            Assert.AreEqual(0, _fixture.StructuredLogger.LogCalls.Count);
        }
    }
}
