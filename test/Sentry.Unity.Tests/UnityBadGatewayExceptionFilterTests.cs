using System;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
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

        [Test]
        public void Init_WithDefaultOptions_DoesNotSendBadGatewayExceptions()
        {
            LogAssert.ignoreFailingMessages = true; // The TestHttpClientHandler will complain about timing out (and it should!)

            using var _ = SentryTests.InitSentrySdk(testHttpClientHandler: _testHttpClientHandler);

            SentrySdk.CaptureException(new Exception(UnityBadGatewayExceptionFilter.Message + _identifyingEventValue));

            var createdEvent = _testHttpClientHandler.GetEvent(_identifyingEventValue, _eventReceiveTimeout);
            Assert.AreEqual(string.Empty, createdEvent);
        }

        internal IDisposable InitSentrySdk(Action<SentryUnityOptions>? configure = null)
        {
            SentryUnity.Init(options =>
            {
                options.Dsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880";
                options.CreateHttpMessageHandler = () => _testHttpClientHandler;

                configure?.Invoke(options);
            });

            return new SentryDisposable();
        }

        private sealed class SentryDisposable : IDisposable
        {
            public void Dispose() => SentrySdk.Close();
        }
    }
}
