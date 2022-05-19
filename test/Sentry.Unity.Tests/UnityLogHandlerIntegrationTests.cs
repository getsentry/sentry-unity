using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Protocol;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public sealed class UnityLogHandlerIntegrationTests
    {
        private class Fixture
        {
            public TestHub Hub { get; set; } = null!;
            public SentryUnityOptions SentryOptions { get; set; } = null!;

            public UnityLogHandlerIntegration GetSut()
            {
                var application = new TestApplication();
                var integration = new UnityLogHandlerIntegration(application);
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
        public void CaptureLogFormat_LogStartsWithUnityLoggerPrefix_NotCaptured()
        {
            var sut = _fixture.GetSut();
            var message = $"{UnityLogger.LogPrefix}Test Message";

            sut.CaptureLogFormat(LogType.Error, null, "{0}", message);

            Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
        }

        [Test]
        public void CaptureLogFormat_LogTypeError_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            var message = "Test Message";

            sut.CaptureLogFormat(LogType.Error, null, "{0}", message);

            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
            Assert.NotNull(_fixture.Hub.CapturedEvents[0].Message);
            Assert.AreEqual( message, _fixture.Hub.CapturedEvents[0].Message!.Message);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void CaptureLogFormat_LogDebounceEnabled_DebouncesMessage(LogType unityLogType)
        {
            _fixture.SentryOptions.EnableLogDebouncing = true;
            var sut = _fixture.GetSut();
            var message = "message";

            sut.CaptureLogFormat(unityLogType, null, "{0}", message);
            sut.CaptureLogFormat(unityLogType, null, "{0}", message);

            Assert.AreEqual(1, _fixture.Hub.ConfigureScopeCalls.Count);
        }

        private static readonly object[] LogTypesCaptured =
        {
            new object[] { LogType.Error, SentryLevel.Error, BreadcrumbLevel.Error },
            new object[] { LogType.Assert, SentryLevel.Error, BreadcrumbLevel.Error }
        };

        [TestCaseSource(nameof(LogTypesCaptured))]
        public void CaptureLogFormat_UnityErrorLogTypes_CapturedAndCorrespondToSentryLevel(LogType unityLogType, SentryLevel sentryLevel, BreadcrumbLevel breadcrumbLevel)
        {
            var sut = _fixture.GetSut();
            var message = "Test Message";

            sut.CaptureLogFormat(unityLogType, null, "{0}", message);

            var scope = new Scope(_fixture.SentryOptions);
            _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.NotNull(_fixture.Hub.CapturedEvents.SingleOrDefault(capturedEvent => capturedEvent.Level == sentryLevel));
            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual("unity.logger", breadcrumb.Category);
            Assert.AreEqual(breadcrumbLevel, breadcrumb.Level);
        }

        private static readonly object[] LogTypesNotCaptured =
        {
            new object[] { LogType.Log, BreadcrumbLevel.Info },
            new object[] { LogType.Warning, BreadcrumbLevel.Warning }
        };

        [TestCaseSource(nameof(LogTypesNotCaptured))]
        public void CaptureLogFormat_UnityNotErrorLogTypes_NotCaptured(LogType unityLogType, BreadcrumbLevel breadcrumbLevel)
        {
            var sut = _fixture.GetSut();
            var message = "Test Message";

            sut.CaptureLogFormat(unityLogType, null, "{0}", message);

            var scope = new Scope(_fixture.SentryOptions);
            _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual("unity.logger", breadcrumb.Category);
            Assert.AreEqual(breadcrumbLevel, breadcrumb.Level);
        }

        [Test]
        public void CaptureException_ExceptionCapturedAndMechanismSet()
        {
            var sut = _fixture.GetSut();
            var message = "Test Exception";

            sut.CaptureException(new Exception(message), null);

            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);

            var capturedEvent = _fixture.Hub.CapturedEvents.Single();
            Assert.NotNull(capturedEvent);

            Assert.NotNull(capturedEvent.Exception);
            Assert.AreEqual(message, capturedEvent.Exception!.Message);

            Assert.IsTrue(capturedEvent.Exception!.Data.Contains(Mechanism.HandledKey));
            Assert.IsFalse((bool)capturedEvent.Exception!.Data[Mechanism.HandledKey]);

            Assert.IsTrue(capturedEvent.Exception!.Data.Contains(Mechanism.MechanismKey));
            Assert.AreEqual("Unity.LogException",(string)capturedEvent.Exception!.Data[Mechanism.MechanismKey]);
        }

        [Test]
        public void CaptureException_CapturedExceptionAddedAsBreadcrumb()
        {
            var sut = _fixture.GetSut();
            var message = "Test Message";

            sut.CaptureException(new Exception(message), null);

            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count); // Sanity check

            var scope = new Scope(_fixture.SentryOptions);
            _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual("unity.logger", breadcrumb.Category);
            Assert.AreEqual(BreadcrumbLevel.Error, breadcrumb.Level);
        }
    }
}
