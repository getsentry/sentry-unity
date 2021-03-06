﻿using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public sealed class UnityApplicationLoggingIntegrationTests
    {
        private class Fixture
        {
            public UnityApplicationLoggingIntegration GetSut(IHub hub, SentryOptions sentryOptions)
            {
                var application = new TestApplication();
                var integration = new UnityApplicationLoggingIntegration(application);
                integration.Register(hub, sentryOptions);
                return integration;
            }
        }

        private Fixture _fixture = null!;
        private TestHub _hub = null!;
        private SentryOptions _sentryOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            _hub = new TestHub();
            _sentryOptions = new SentryOptions();
        }

        [Test]
        public void OnLogMessageReceived_WithError_CaptureEvent()
        {
            var sut = _fixture.GetSut(_hub, _sentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);

            Assert.AreEqual(1, _hub.CapturedEvents.Count);
        }

        [Test]
        public void OnLogMessageReceived_WithSeveralErrorsDebounced_CaptureEvent()
        {
            var sut = _fixture.GetSut(_hub, _sentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);
            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);

            Assert.AreEqual(1, _hub.CapturedEvents.Count);
        }

        [Test]
        public void OnLogMessageReceived_Breadcrumbs_Added()
        {
            var sut = _fixture.GetSut(_hub, _sentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Warning);
            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);

            Assert.AreEqual(2, _hub.ConfigureScopeCalls.Count);
        }

        [TestCaseSource(nameof(LogTypesAndSentryLevels))]
        public void OnLogMessageReceived_UnityErrorLogTypes_CapturedAndCorrespondToSentryLevel(LogType unityLogType, SentryLevel sentryLevel, BreadcrumbLevel breadcrumbLevel)
        {
            var sut = _fixture.GetSut(_hub, _sentryOptions);
            var condition = "condition";

            sut.OnLogMessageReceived(condition, "stacktrace", unityLogType);

            var configureScope = _hub.ConfigureScopeCalls.Single();
            var scope = new Scope(_sentryOptions);
            configureScope(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.NotNull(_hub.CapturedEvents.SingleOrDefault(capturedEvent => capturedEvent.Level == sentryLevel));
            Assert.AreEqual(condition, breadcrumb.Message);
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
        public void OnLogMessageReceived_UnityNotErrorLogTypes_NotCaptured(LogType unityLogType, BreadcrumbLevel breadcrumbLevel)
        {
            var sut = _fixture.GetSut(_hub, _sentryOptions);
            var condition = "condition";

            sut.OnLogMessageReceived(condition, "stacktrace", unityLogType);

            var configureScope = _hub.ConfigureScopeCalls.Single();
            var scope = new Scope(_sentryOptions);
            configureScope(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual(0, _hub.CapturedEvents.Count);
            Assert.AreEqual(condition, breadcrumb.Message);
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
