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

            public bool CaptureExceptions { get; set; } = false;

            public UnityApplicationLoggingIntegration GetSut()
            {
                var application = new TestApplication();
                var integration = new UnityApplicationLoggingIntegration(CaptureExceptions, application, clock: null);
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
        public void OnLogMessageReceived_LogContainsSentryTag_NotCaptured()
        {
            var sut = _fixture.GetSut();
            var message = $"{UnityLogger.LogTag}: {TestContext.CurrentContext.Test.Name}";

            sut.OnLogMessageReceived(message, string.Empty, LogType.Error);

            Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void OnLogMessageReceived_LogTypeError_CaptureEvent(bool captureLogErrorEvents)
        {
            _fixture.SentryOptions.CaptureLogErrorEvents = captureLogErrorEvents;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, LogType.Error);

            if (captureLogErrorEvents)
            {
                Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
                Assert.NotNull(_fixture.Hub.CapturedEvents[0].Message);
                Assert.AreEqual(message, _fixture.Hub.CapturedEvents[0].Message!.Message);
            }
            else
            {
                Assert.AreEqual(0, _fixture.Hub.CapturedEvents.Count);
            }
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void OnLogMessageReceived_LogDebounceEnabled_DebouncesMessage(LogType unityLogType)
        {
            _fixture.SentryOptions.EnableLogDebouncing = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            Assert.AreEqual(1, _fixture.Hub.ConfigureScopeCalls.Count);
        }

        private static readonly object[] LogTypesCaptured = [new object[] { LogType.Error, SentryLevel.Error, BreadcrumbLevel.Error }];

        [TestCaseSource(nameof(LogTypesCaptured))]
        public void OnLogMessageReceived_UnityErrorLogTypes_CapturedAndCorrespondToSentryLevel(LogType unityLogType, SentryLevel sentryLevel, BreadcrumbLevel breadcrumbLevel)
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
        public void OnLogMessageReceived_UnityNotErrorLogTypes_NotCaptured(LogType unityLogType, BreadcrumbLevel breadcrumbLevel)
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
        public void OnLogMessageReceived_AddAsBreadcrumbEnabled_AddedAsBreadcrumb(LogType unityLogType)
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
        public void OnLogMessageReceived_AddAsBreadcrumbDisabled_NotAddedAsBreadcrumb(LogType unityLogType)
        {
            _fixture.SentryOptions.AddBreadcrumbsForLogType[unityLogType] = false;
            var sut = _fixture.GetSut();
            var message = NUnit.Framework.TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            Assert.IsFalse(_fixture.Hub.ConfigureScopeCalls.Count > 0);
        }

        [Test]
        public void OnLogMessageReceived_LogTypeException_CaptureExceptionsEnabled_EventCaptured()
        {
            _fixture.CaptureExceptions = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, "stacktrace", LogType.Exception);

            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void OnLogMessageReceived_ExperimentalLogsEnabledWithAttachBreadcrumbsFalse_BreadcrumbsNotAdded(LogType unityLogType)
        {
            _fixture.SentryOptions.Experimental.EnableLogs = true;
            _fixture.SentryOptions.Experimental.AttachBreadcrumbsToEvents = false;
            _fixture.SentryOptions.AddBreadcrumbsForLogType[unityLogType] = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            var scope = new Scope(_fixture.SentryOptions);
            foreach (var configureScopeCall in _fixture.Hub.ConfigureScopeCalls)
            {
                configureScopeCall.Invoke(scope);
            }

            Assert.AreEqual(0, scope.Breadcrumbs.Count);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void OnLogMessageReceived_ExperimentalLogsEnabledWithAttachBreadcrumbsTrue_BreadcrumbsAdded(LogType unityLogType)
        {
            _fixture.SentryOptions.Experimental.EnableLogs = true;
            _fixture.SentryOptions.Experimental.AttachBreadcrumbsToEvents = true;
            _fixture.SentryOptions.AddBreadcrumbsForLogType[unityLogType] = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            var scope = new Scope(_fixture.SentryOptions);
            foreach (var configureScopeCall in _fixture.Hub.ConfigureScopeCalls)
            {
                configureScopeCall.Invoke(scope);
            }

            Assert.AreEqual(1, scope.Breadcrumbs.Count);
            Assert.AreEqual(message, scope.Breadcrumbs.Single().Message);
            Assert.AreEqual("unity.logger", scope.Breadcrumbs.Single().Category);
        }

        [Test]
        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        public void OnLogMessageReceived_ExperimentalLogsDisabled_BreadcrumbsAddedAsNormal(LogType unityLogType)
        {
            _fixture.SentryOptions.Experimental.EnableLogs = false;
            _fixture.SentryOptions.AddBreadcrumbsForLogType[unityLogType] = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, unityLogType);

            var scope = new Scope(_fixture.SentryOptions);
            _fixture.Hub.ConfigureScopeCalls.Single().Invoke(scope);
            var breadcrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual(message, breadcrumb.Message);
            Assert.AreEqual("unity.logger", breadcrumb.Category);
        }

        [Test]
        public void OnLogMessageReceived_ExceptionType_NoBreadcrumbAdded()
        {
            _fixture.SentryOptions.AddBreadcrumbsForLogType[LogType.Exception] = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, "stacktrace", LogType.Exception);

            // Exception breadcrumbs are handled by the .NET SDK, not by this integration
            Assert.AreEqual(0, _fixture.Hub.ConfigureScopeCalls.Count);
        }

        [Test]
        public void OnLogMessageReceived_LogErrorAttachStackTraceTrue_CapturesMessageWithThread()
        {
            _fixture.SentryOptions.AttachStacktrace = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;
            var stacktrace = "BugFarmButtons:LogError () (at Assets/Scripts/BugFarmButtons.cs:85)";

            sut.OnLogMessageReceived(message, stacktrace, LogType.Error);

            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
            var capturedEvent = _fixture.Hub.CapturedEvents[0];

            Assert.NotNull(capturedEvent.Message);
            Assert.AreEqual(message, capturedEvent.Message!.Message);
            Assert.IsEmpty(capturedEvent.SentryExceptions);

            Assert.NotNull(capturedEvent.SentryThreads);
            var thread = capturedEvent.SentryThreads.Single();
            Assert.NotNull(thread.Stacktrace);
            Assert.NotNull(thread.Stacktrace!.Frames);
            Assert.Greater(thread.Stacktrace.Frames.Count, 0);
        }

        [Test]
        public void OnLogMessageReceived_LogErrorAttachStackTraceFalse_CaptureMessageWithNoStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = false;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, "stacktrace", LogType.Error);

            Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
            var capturedEvent = _fixture.Hub.CapturedEvents[0];

            Assert.NotNull(capturedEvent.Message);
            Assert.AreEqual(message, capturedEvent.Message!.Message);
            Assert.IsEmpty(capturedEvent.SentryExceptions);
            Assert.IsEmpty(capturedEvent.SentryThreads);
        }

        [Test]
        public void OnLogMessageReceived_ExperimentalCaptureEnabled_CapturesStructuredLog()
        {
            _fixture.SentryOptions.Experimental.EnableLogs = true;
            _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Exception] = true;
            _fixture.CaptureExceptions = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, LogType.Exception);

            var logger = (TestStructuredLogger)_fixture.Hub.Logger;
            Assert.AreEqual(1, logger.CapturedLogs.Count);
            var log = logger.CapturedLogs[0];
            Assert.AreEqual(SentryLogLevel.Error, log.Level);
            Assert.AreEqual(message, log.Message);
        }

        [Test]
        public void OnLogMessageReceived_ExperimentalCaptureDisabled_DoesNotCaptureStructuredLog()
        {
            _fixture.SentryOptions.Experimental.EnableLogs = true;
            _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Exception] = false;
            _fixture.CaptureExceptions = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, LogType.Exception);

            var logger = (TestStructuredLogger)_fixture.Hub.Logger;
            Assert.AreEqual(0, logger.CapturedLogs.Count);
        }

        [Test]
        public void OnLogMessageReceived_WithSentryLogTag_DoesNotCaptureStructuredLog()
        {
            _fixture.SentryOptions.Experimental.EnableLogs = true;
            _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Error] = true;
            var sut = _fixture.GetSut();
            var message = $"{UnityLogger.LogTag}: Test message";

            sut.OnLogMessageReceived(message, string.Empty, LogType.Error);

            var logger = (TestStructuredLogger)_fixture.Hub.Logger;
            Assert.AreEqual(0, logger.CapturedLogs.Count);
        }

        [Test]
        public void OnLogMessageReceived_WithEnableLogsFalse_DoesNotCaptureStructuredLog()
        {
            _fixture.SentryOptions.Experimental.EnableLogs = false;
            _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[LogType.Error] = true;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, LogType.Error);

            var logger = (TestStructuredLogger)_fixture.Hub.Logger;
            Assert.AreEqual(0, logger.CapturedLogs.Count);
        }

        [Test]
        [TestCase(LogType.Log, SentryLogLevel.Info, true)]
        [TestCase(LogType.Log, SentryLogLevel.Info, false)]
        [TestCase(LogType.Warning, SentryLogLevel.Warning, true)]
        [TestCase(LogType.Warning, SentryLogLevel.Warning, false)]
        [TestCase(LogType.Error, SentryLogLevel.Error, true)]
        [TestCase(LogType.Error, SentryLogLevel.Error, false)]
        [TestCase(LogType.Assert, SentryLogLevel.Error, true)]
        [TestCase(LogType.Assert, SentryLogLevel.Error, false)]
        public void OnLogMessageReceived_WithExperimentalFlag_CapturesStructuredLogWhenEnabled(LogType logType, SentryLogLevel expectedLevel, bool captureEnabled)
        {
            _fixture.SentryOptions.Experimental.EnableLogs = true;
            _fixture.SentryOptions.Experimental.CaptureStructuredLogsForLogType[logType] = captureEnabled;
            var sut = _fixture.GetSut();
            var message = TestContext.CurrentContext.Test.Name;

            sut.OnLogMessageReceived(message, string.Empty, logType);

            var logger = (TestStructuredLogger)_fixture.Hub.Logger;
            if (captureEnabled)
            {
                Assert.AreEqual(1, logger.CapturedLogs.Count);
                var log = logger.CapturedLogs[0];
                Assert.AreEqual(expectedLevel, log.Level);
                Assert.AreEqual(message, log.Message);
            }
            else
            {
                Assert.AreEqual(0, logger.CapturedLogs.Count);
            }
        }
    }
}
