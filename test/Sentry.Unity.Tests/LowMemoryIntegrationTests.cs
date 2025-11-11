using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests;

public class LowMemoryIntegrationTests
{
    private class Fixture
    {
        public TestApplication Application { get; set; } = new();
        public TestHub Hub { get; set; } = new();
        public SentryUnityOptions Options { get; set; } = new();

        public LowMemoryIntegration GetSut() => new(Application);
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
    }
    
    [Test]
    public void LowMemory_EnabledHub_BreadcrumbAdded()
    {
        var sut = _fixture.GetSut();

        sut.Register(_fixture.Hub, _fixture.Options);
        _fixture.Application.OnLowMemory();

        var configureScope = _fixture.Hub.ConfigureScopeCalls.Single();
        var scope = new Scope(_fixture.Options);
        configureScope(scope);
        var actualCrumb = scope.Breadcrumbs.Single();

        Assert.AreEqual("Low memory", actualCrumb.Message);
        Assert.AreEqual("device.event", actualCrumb.Category);
        Assert.AreEqual("system", actualCrumb.Type);
        Assert.AreEqual(BreadcrumbLevel.Warning, actualCrumb.Level);
        Assert.NotNull(actualCrumb.Data);
        Assert.AreEqual("LOW_MEMORY", actualCrumb.Data?["action"]);
    }

    [Test]
    public void LowMemory_MultipleTriggers_MultipleBreadcrumbsAdded()
    {
        var sut = _fixture.GetSut();

        sut.Register(_fixture.Hub, _fixture.Options);
        _fixture.Application.OnLowMemory();
        _fixture.Application.OnLowMemory();
        _fixture.Application.OnLowMemory();

        Assert.AreEqual(3, _fixture.Hub.ConfigureScopeCalls.Count);
    }
}
