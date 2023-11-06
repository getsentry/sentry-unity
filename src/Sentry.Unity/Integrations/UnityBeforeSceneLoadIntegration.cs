using System.Collections.Generic;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations
{
    internal sealed class UnityBeforeSceneLoadIntegration : ISdkIntegration
    {
        private readonly IApplication _application;

        public UnityBeforeSceneLoadIntegration(IApplication? application = null)
            => _application = application ?? ApplicationAdapter.Instance;

        public void Register(IHub hub, SentryOptions options)
        {
            var data = _application.ActiveSceneName is { } name
                ? new Dictionary<string, string> { { "scene", name } }
                : null;

            hub.AddBreadcrumb(message: "BeforeSceneLoad", category: "scene.beforeload", data: data);
        }
    }
}
