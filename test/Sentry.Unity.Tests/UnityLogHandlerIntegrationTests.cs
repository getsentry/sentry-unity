using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public sealed class UnityLogHandlerIntegrationTests
    {
        private class Fixture
        {
            public TestHub Hub { get; set; }= null!;
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
        public void LogFormat_LogStartsWithUnityLoggerPrefix_NotCaptured()
        {
            var sut = _fixture.GetSut();
            var message = $"{UnityLogger.LogPrefix}message";

            sut.LogFormat(LogType.Error, null, "{0}", message);

            LogAssert.Expect(LogType.Error, message);
            Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
        }

        [Test]
        public void LogFormat_WithError_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            var message = "message";

            sut.LogFormat(LogType.Error, null, "{0}", message);

            LogAssert.Expect(LogType.Error, message);
            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
        }

        [Test]
        public void LogFormat_Breadcrumbs_Added()
        {
            var sut = _fixture.GetSut();
            var message = "message";

            sut.LogFormat(LogType.Warning, null, "{0}", message);
            LogAssert.Expect(LogType.Warning, message);
            sut.LogFormat(LogType.Error, null, "{0}", message);
            LogAssert.Expect(LogType.Error, message);

            Assert.AreEqual(2, _fixture.Hub.ConfigureScopeCalls.Count);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void LogFormat_LogDebounceEnabled_DebouncesMessage(LogType unityLogType)
        {
            _fixture.SentryOptions.EnableLogDebouncing = true;
            var sut = _fixture.GetSut();
            var message = "message";

            sut.LogFormat(unityLogType, null, "{0}", message);
            LogAssert.Expect(unityLogType, message);
            sut.LogFormat(unityLogType, null, "{0}", message);
            LogAssert.Expect(unityLogType, message);

            Assert.AreEqual(1, _fixture.Hub.ConfigureScopeCalls.Count);
        }

        [TestCaseSource(nameof(LogTypesAndSentryLevels))]
        public void LogFormat_UnityErrorLogTypes_CapturedAndCorrespondToSentryLevel(LogType unityLogType, SentryLevel sentryLevel, BreadcrumbLevel breadcrumbLevel)
        {
            var sut = _fixture.GetSut();
            var message = "message";

            sut.LogFormat(unityLogType, null, "{0}", message);
            LogAssert.Expect(unityLogType, message);

            var configureScope = _fixture.Hub.ConfigureScopeCalls.Single();
            var scope = new Scope(_fixture.SentryOptions);
            configureScope(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.NotNull(_fixture.Hub.CapturedEvents.SingleOrDefault(capturedEvent => capturedEvent.Level == sentryLevel));
            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual("unity.logger", breadcrumb.Category);
            Assert.AreEqual(breadcrumbLevel, breadcrumb.Level);
        }

        private static readonly object[] LogTypesAndSentryLevels =
        {
            new object[] { LogType.Error, SentryLevel.Error, BreadcrumbLevel.Error },
            new object[] { LogType.Exception, SentryLevel.Error, BreadcrumbLevel.Error },
            new object[] { LogType.Assert, SentryLevel.Error, BreadcrumbLevel.Error }
        };

        [TestCaseSource(nameof(LogTypesNotCaptured))]
        public void LogFormat_UnityNotErrorLogTypes_NotCaptured(LogType unityLogType, BreadcrumbLevel breadcrumbLevel)
        {
            var sut = _fixture.GetSut();
            var message = "message";

            sut.LogFormat(unityLogType, null, "{0}", message);
            LogAssert.Expect(unityLogType, message);

            var configureScope = _fixture.Hub.ConfigureScopeCalls.Single();
            var scope = new Scope(_fixture.SentryOptions);
            configureScope(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual("unity.logger", breadcrumb.Category);
            Assert.AreEqual(breadcrumbLevel, breadcrumb.Level);
        }

        private static readonly object[] LogTypesNotCaptured =
        {
            new object[] { LogType.Log, BreadcrumbLevel.Info },
            new object[] { LogType.Warning, BreadcrumbLevel.Warning }
        };
    }
}
