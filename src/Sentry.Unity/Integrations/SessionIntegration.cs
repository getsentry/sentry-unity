using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations
{
    internal class SessionIntegration : ISdkIntegration
    {
        private readonly SentryMonoBehaviour _sentryMonoBehaviour;

        public SessionIntegration(SentryMonoBehaviour sentryMonoBehaviour)
        {
            _sentryMonoBehaviour = sentryMonoBehaviour;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            if (!options.AutoSessionTracking)
            {
                return;
            }

            _sentryMonoBehaviour.ApplicationResuming += () =>
            {
                options.DiagnosticLogger?.LogDebug("Resuming session.");
                hub.ResumeSession();
            };
            _sentryMonoBehaviour.ApplicationPausing += () =>
            {
                options.DiagnosticLogger?.LogDebug("Pausing session.");
                hub.PauseSession();
            };
        }
    }
}
