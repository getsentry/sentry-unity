using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Internal;
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

        public UnityLogHandlerIntegration GetSut()
        {
            var integration = new UnityLogHandlerIntegration();
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
        var message = "test message" + Guid.NewGuid();

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

        Assert.IsTrue(capturedEvent.Exception!.Data.Contains(Mechanism.TerminalKey));
        Assert.IsFalse((bool)capturedEvent.Exception!.Data[Mechanism.TerminalKey]);
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
}
