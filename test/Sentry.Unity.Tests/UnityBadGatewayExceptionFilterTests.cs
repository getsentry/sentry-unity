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
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880";
            options.CreateHttpMessageHandler = () => _testHttpClientHandler;

            configure?.Invoke(options);
        });

        return new SentryDisposable();
    }

    private sealed class SentryDisposable : IDisposable
    {
        public void Dispose() => Sentry.SentrySdk.Close();
    }
}
