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
    }
}
