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
            Assert.IsTrue(new UnityBadGatewayExceptionFilter().Filter(new Exception(UnitySocketExceptionFilter.Message)));


    }
}
