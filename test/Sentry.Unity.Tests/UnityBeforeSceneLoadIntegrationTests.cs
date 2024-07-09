using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests;

public sealed class UnityBeforeSceneLoadIntegrationTests
{
    private class Fixture
    {
        public UnityBeforeSceneLoadIntegration GetSut(IHub hub, SentryOptions sentryOptions)
        {
            var application = new TestApplication();
            var integration = new UnityBeforeSceneLoadIntegration(application);
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
    public void Register_Breadcrumb_Added()
    {
        _ = _fixture.GetSut(_hub, _sentryOptions);

        var configureScope = _hub.ConfigureScopeCalls.Single();
        var scope = new Scope(_sentryOptions);
        configureScope(scope);
        var breadcrumb = scope.Breadcrumbs.Single();

        Assert.AreEqual(1, _hub.ConfigureScopeCalls.Count);
        Assert.AreEqual("scene.beforeload", breadcrumb.Category);
    }
}
