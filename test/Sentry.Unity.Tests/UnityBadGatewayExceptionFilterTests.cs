using System;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class UnityBadGatewayExceptionFilterTests
{
    private TestHttpClientHandler _testHttpClientHandler = null!; // Set in Setup
    private readonly TimeSpan _eventReceiveTimeout = TimeSpan.FromSeconds(0.5f);

    private string _identifyingEventValue = null!; // Set in Setup

    [SetUp]
    public void SetUp()
    {
        _testHttpClientHandler = new TestHttpClientHandler("SetupTestHttpClientHandler");
        _identifyingEventValue = Guid.NewGuid().ToString();
    }

    [Test]
    public void Filter_FiltersBadGatewayExceptionsOfTypeException() =>

        Assert.IsTrue(new UnityBadGatewayExceptionFilter().Filter(new Exception(UnityBadGatewayExceptionFilter.Message)));

    internal IDisposable InitSentrySdk(Action<SentryUnityOptions>? configure = null)
        => SentryTests.InitSentrySdk(configure, _testHttpClientHandler);
}
