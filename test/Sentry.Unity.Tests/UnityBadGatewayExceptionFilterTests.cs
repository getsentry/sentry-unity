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
            Assert.IsTrue(new UnityBadGatewayExceptionFilter().Filter(new Exception("Error: HTTP/1.1 502 Bad Gateway")));

        [Test]
        public void Init_WithDefaultOptions_DoesNotSendBadGatewayExceptions()
        {
            LogAssert.ignoreFailingMessages = true; // The TestHttpClientHandler will complain about timing out (and it should!)

            using var _ = SentryTests.InitSentrySdk(testHttpClientHandler:_testHttpClientHandler);

            SentrySdk.CaptureException(new Exception("Error: HTTP/1.1 502 Bad Gateway" + _identifyingEventValue));

            var createdEvent = _testHttpClientHandler.GetEvent(_identifyingEventValue, _eventReceiveTimeout);
            Assert.AreEqual(string.Empty, createdEvent);
        }
    }
}
