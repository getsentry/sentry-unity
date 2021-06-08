using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests
{
    public sealed class UnityBeforeSceneLoadIntegrationTests
    {
        private class Fixture
        {
            public TestHub Hub => new();
            public IApplication Application => new TestApplication();

            public UnityBeforeSceneLoadIntegration GetSut() => new(Application);
        }

        private readonly Fixture _fixture = new();

        private SentryOptions SentryOptions { get; set; } = new();

        [Test]
        public void Register_Breadcrumb_Added()
        {
            var sut = _fixture.GetSut();
            var hub = _fixture.Hub;

            sut.Register(hub, SentryOptions);

            Assert.AreEqual(1, hub.ConfigureScopeCalls.Count);
        }
    }
}
