using System;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public class UnitySocketExceptionFilterTests
    {
        private TestHttpClientHandler _testHttpClientHandler = null!; // Set in Setup
        private readonly TimeSpan _eventReceiveTimeout = TimeSpan.FromSeconds(0.5f);

        [SetUp]
        public void SetUp() =>
            _testHttpClientHandler = new TestHttpClientHandler("SetupTestHttpClientHandler");

        [Test]
        public void Filter_FiltersBadGatewayExceptionsOfTypeException() =>
            Assert.IsTrue(new UnitySocketExceptionFilter().Filter(new System.Net.Sockets.SocketException(10049)));

        [Test]
        public void Init_WithDefaultOptions_DoesNotSendFilteredSocketExceptions()
        {
            LogAssert.ignoreFailingMessages = true; // The TestHttpClientHandler will complain about timing out (and it should!)

            using var _ = SentryTests.InitSentrySdk(testHttpClientHandler: _testHttpClientHandler);

            SentrySdk.CaptureException(new System.Net.Sockets.SocketException(10049)); // The requested address is not valid in this context

            var createdEvent = _testHttpClientHandler.GetEvent(UnitySocketExceptionFilter.Message, _eventReceiveTimeout);
            Assert.AreEqual(string.Empty, createdEvent);
        }
    }
}
