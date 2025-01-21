using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Protocol;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public sealed class UnityLogHandlerIntegrationTests
{
    private class Fixture
    {
        public TestHub Hub { get; set; } = null!;
        public SentryUnityOptions SentryOptions { get; set; } = null!;

        public UnityLogHandlerIntegration GetSut()
        {
            var application = new TestApplication();
            var integration = new UnityLogHandlerIntegration(SentryOptions, application);
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
    public void CaptureException_AddAsBreadcrumbEnabled_AddedAsBreadcrumb()
    {
        _fixture.SentryOptions.AddBreadcrumbsForLogType[LogType.Exception] = true;
        var sut = _fixture.GetSut();
        var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

        sut.CaptureException(new Exception(message), null);

        var scope = new Scope(_fixture.SentryOptions);
        _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
        var breadcrumb = scope.Breadcrumbs.Single();

        StringAssert.Contains(message, breadcrumb.Message);
    }

    [Test]
    public void CaptureException_AddAsBreadcrumbEnabled_NotAddedAsBreadcrumb()
    {
        _fixture.SentryOptions.AddBreadcrumbsForLogType[LogType.Exception] = false;
        var sut = _fixture.GetSut();
        var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

        sut.CaptureException(new Exception("Test Exception"), null);

        Assert.IsFalse(_fixture.Hub.ConfigureScopeCalls.Count > 0);
    }

    [Test]
    public void CaptureException_ExceptionCapturedAndMechanismSet()
    {
        var sut = _fixture.GetSut();
        var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

        sut.CaptureException(new Exception(message), null);

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
    public void CaptureException_CapturedExceptionAddedAsBreadcrumb()
    {
        var sut = _fixture.GetSut();
        var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;
        var exception = new Exception(message);

        sut.CaptureException(exception, null);

        Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count); // Sanity check

        var scope = new Scope(_fixture.SentryOptions);
        _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
        var breadcrumb = scope.Breadcrumbs.Single();

        Assert.AreEqual(exception.GetType() + ": " + message, breadcrumb.Message);
        Assert.AreEqual("unity.logger", breadcrumb.Category);
        Assert.AreEqual(BreadcrumbLevel.Error, breadcrumb.Level);
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
