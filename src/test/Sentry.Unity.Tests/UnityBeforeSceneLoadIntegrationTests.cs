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
            public IAppDomain AppDomain => new TestAppDomain();

            public UnityBeforeSceneLoadIntegration GetSut() => new(AppDomain);
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
