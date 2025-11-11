using System.Collections.Generic;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

internal class LowMemoryIntegration : ISdkIntegration
{
    private IApplication _application;

    public LowMemoryIntegration(IApplication? application = null)
    {
        _application = application ?? ApplicationAdapter.Instance;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        _application.LowMemory += () =>
        {
            var breadcrumb = new Breadcrumb(
                message: "Low memory",
                type: "system",
                data: new Dictionary<string, string> { { "action", "LOW_MEMORY" } },
                category: "device.event",
                level: BreadcrumbLevel.Warning);
            hub.AddBreadcrumb(breadcrumb);
        };
    }
}
