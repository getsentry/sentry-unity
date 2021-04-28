using System.Collections.Generic;
using Sentry.Integrations;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    internal sealed class UnityBeforeSceneLoadIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            var data = SceneManager.GetActiveScene().name is { } name
                ? new Dictionary<string, string> {{"scene", name}}
                : null;

            SentrySdk.AddBreadcrumb("BeforeSceneLoad", data: data);

            options.DiagnosticLogger?.Log(SentryLevel.Debug, "Complete Sentry SDK initialization.");
        }
    }
}
