using Sentry.Extensibility;
using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity.Integrations
{
    public class SessionIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            if (!options.AutoSessionTracking)
            {
                return;
            }

            options.DiagnosticLogger?.LogDebug("Registering Session integration.");

            // HideFlags.HideAndDontSave hides the GameObject in the hierarchy and prevents changing of scenes from destroying it
            var gameListenerObject = new GameObject("SentryListener") {hideFlags = HideFlags.HideAndDontSave};
            var gameListener = gameListenerObject.AddComponent<SentryMonoBehaviour>();
            gameListener.ApplicationResuming += () =>
            {
                options.DiagnosticLogger?.LogDebug("Resuming session.");
                hub.ResumeSession();
            };
            gameListener.ApplicationPausing += () =>
            {
                options.DiagnosticLogger?.LogDebug("Pausing session.");
                hub.PauseSession();
            };
        }
    }
}
