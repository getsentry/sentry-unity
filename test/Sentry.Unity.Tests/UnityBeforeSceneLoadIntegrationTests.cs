using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests
{
    public sealed class UnityBeforeSceneLoadIntegrationTests
    {
        private class Fixture
        {
            public Stubs.TestHub Hub => new();
            public IApplication Application => new TestApplication();

            public UnityBeforeSceneLoadIntegration GetSut() => new(Application);
        }

        private readonly Fixture _fixture = new();

        private SentryOptions SentryOptions { get; set; } = new();

        // TODO: How to stub Scope with Breadcrumbs?
        public void BreadcrumbSceneName()
        {
            var sut = _fixture.GetSut();
            var hub = _fixture.Hub;
            sut.Register(hub, SentryOptions);
        }
    }
}
