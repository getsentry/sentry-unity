using System;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class UnityWebExceptionFilterTests
{
    private TestHttpClientHandler _testHttpClientHandler = null!; // Set in Setup
    private readonly TimeSpan _eventReceiveTimeout = TimeSpan.FromSeconds(0.5f);

    [SetUp]
    public void SetUp() =>
        _testHttpClientHandler = new TestHttpClientHandler("SetupTestHttpClientHandler");

    [Test]
    public void Filter_FiltersBadGatewayExceptionsOfTypeException() =>
        Assert.IsTrue(new UnityWebExceptionFilter().Filter(new System.Net.WebException(UnityWebExceptionFilter.Message)));

    [Test]
    public void Init_WithDefaultOptions_DoesNotSendFilteredWebExceptions()
    {
        LogAssert.ignoreFailingMessages = true; // The TestHttpClientHandler will complain about timing out (and it should!)

        using var _ = SentryTests.InitSentrySdk(testHttpClientHandler: _testHttpClientHandler);

        SentrySdk.CaptureException(new System.Net.WebException(UnityWebExceptionFilter.Message));

        var createdEvent = _testHttpClientHandler.GetEvent(UnityWebExceptionFilter.Message, _eventReceiveTimeout);
        Assert.AreEqual(string.Empty, createdEvent);
    }
}
