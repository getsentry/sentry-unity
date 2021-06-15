using System.Linq;
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
            public Stubs.TestHub Hub => new();
            public IApplication Application => new TestApplication();

            public UnityApplicationLoggingIntegration GetSut() => new(Application);
        }

        private readonly Fixture _fixture = new();

        private SentryOptions SentryOptions { get; set; } = new();

        [Test]
        public void OnLogMessageReceived_WithError_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            var hub = _fixture.Hub;
            sut.Register(hub, SentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);

            Assert.AreEqual(1, hub.CapturedEvents.Count);
        }

        [Test]
        public void OnLogMessageReceived_WithSeveralErrorsDebounced_CaptureEvent()
        {
            var sut = _fixture.GetSut();
            var hub = _fixture.Hub;
            sut.Register(hub, SentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);
            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);

            Assert.AreEqual(1, hub.CapturedEvents.Count);
        }

        [Test]
        public void OnLogMessageReceived_Breadcrumbs_Added()
        {
            var sut = _fixture.GetSut();
            var hub = _fixture.Hub;
            sut.Register(hub, SentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Warning);
            sut.OnLogMessageReceived("condition", "stacktrace", LogType.Error);

            Assert.AreEqual(2, hub.ConfigureScopeCalls.Count);
        }

        [TestCaseSource(nameof(LogTypesAndSentryLevels))]
        public void OnLogMessageReceived_UnityErrorLogTypes_CapturedAndCorrespondToSentryLevel(LogType unityLogType, SentryLevel sentryLevel)
        {
            var sut = _fixture.GetSut();
            var hub = _fixture.Hub;
            sut.Register(hub, SentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", unityLogType);

            Assert.NotNull(hub.CapturedEvents.SingleOrDefault(capturedEvent => capturedEvent.Level == sentryLevel));
        }

        private static readonly object[] LogTypesAndSentryLevels =
        {
            new object[] { LogType.Error, SentryLevel.Error },
            new object[] { LogType.Exception, SentryLevel.Error },
            new object[] { LogType.Assert, SentryLevel.Error }
        };

        [TestCaseSource(nameof(LogTypesNotCaptured))]
        public void OnLogMessageReceived_UnityNotErrorLogTypes_NotCaptured(LogType unityLogType)
        {
            var sut = _fixture.GetSut();
            var hub = _fixture.Hub;
            sut.Register(hub, SentryOptions);

            sut.OnLogMessageReceived("condition", "stacktrace", unityLogType);

            Assert.AreEqual(0, hub.CapturedEvents.Count);
        }

        private static readonly object[] LogTypesNotCaptured =
        {
            new object[] { LogType.Log },
            new object[] { LogType.Warning }
        };
    }
}
