using System.Collections.Generic;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations
{
    internal sealed class UnityBeforeSceneLoadIntegration : ISdkIntegration
    {
        private readonly IAppDomain _appDomain;

        public UnityBeforeSceneLoadIntegration(IAppDomain? appDomain = null)
            => _appDomain = appDomain ?? UnityAppDomain.Instance;

        public void Register(IHub hub, SentryOptions options)
        {
            var data = _appDomain.ActiveSceneName is { } name
                ? new Dictionary<string, string> {{"scene", name}}
                : null;

            hub.AddBreadcrumb("BeforeSceneLoad", data: data);

            options.DiagnosticLogger?.Log(SentryLevel.Debug, "Registered BeforeSceneLoad integration.");
        }
    }
}
