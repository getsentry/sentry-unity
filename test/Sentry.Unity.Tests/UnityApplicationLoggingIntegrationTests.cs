using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public class UnityApplicationLoggingIntegrationTests
    {

        private class Fixture
        {
            public TestHub Hub { get; set; } = null!;
            public SentryUnityOptions SentryOptions { get; set; } = null!;

            public UnityApplicationLoggingIntegration GetSut()
            {
                var application = new TestApplication();
                var integration = new UnityApplicationLoggingIntegration(application);
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
        public void CaptureLogFormat_LogContainsSentryTag_NotCaptured()
        {
            var sut = _fixture.GetSut();
            var message = $"{UnityLogger.LogTag}: {TestContext.CurrentContext.Test.Name}";

            sut.OnLogMessageReceived(message, string.Empty, LogType.Error);

            Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
        }

        [Test]
        public void CaptureLogFormat_LogTypeError_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, LogType.Error);

            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
            Assert.NotNull(_fixture.Hub.CapturedEvents[0].Message);
            Assert.AreEqual(message, _fixture.Hub.CapturedEvents[0].Message!.Message);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void CaptureLogFormat_LogDebounceEnabled_DebouncesMessage(LogType unityLogType)
        {
            _fixture.SentryOptions.EnableLogDebouncing = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            Assert.AreEqual(1, _fixture.Hub.ConfigureScopeCalls.Count);
        }

        private static readonly object[] LogTypesCaptured = [new object[] { LogType.Error, SentryLevel.Error, BreadcrumbLevel.Error }];

        [TestCaseSource(nameof(LogTypesCaptured))]
        public void CaptureLogFormat_UnityErrorLogTypes_CapturedAndCorrespondToSentryLevel(LogType unityLogType, SentryLevel sentryLevel, BreadcrumbLevel breadcrumbLevel)
        {
            var sut = _fixture.GetSut();
            var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

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
            var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            var scope = new Scope(_fixture.SentryOptions);
            _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual("unity.logger", breadcrumb.Category);
            Assert.AreEqual(breadcrumbLevel, breadcrumb.Level);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void CaptureLogFormat_AddAsBreadcrumbEnabled_AddedAsBreadcrumb(LogType unityLogType)
        {
            _fixture.SentryOptions.AddBreadcrumbsForLogType[unityLogType] = true;
            var sut = _fixture.GetSut();
            var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            var scope = new Scope(_fixture.SentryOptions);
            _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual(message, breadcrumb.Message);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        [TestCase(LogType.Assert)]
        public void CaptureLogFormat_AddAsBreadcrumbDisabled_NotAddedAsBreadcrumb(LogType unityLogType)
        {
            _fixture.SentryOptions.AddBreadcrumbsForLogType[unityLogType] = false;
            var sut = _fixture.GetSut();
            var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            Assert.IsFalse(_fixture.Hub.ConfigureScopeCalls.Count > 0);
        }
    }
}
