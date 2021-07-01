using System;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations
{
    internal class SessionIntegration : ISdkIntegration
    {
        private readonly Func<SentryMonoBehaviour> _sentryMonoBehaviourGenerator;

        public SessionIntegration(Func<SentryMonoBehaviour> sentryMonoBehaviourGenerator)
        {
            _sentryMonoBehaviourGenerator = sentryMonoBehaviourGenerator;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            if (!options.AutoSessionTracking)
            {
                return;
            }

            options.DiagnosticLogger?.LogDebug("Registering Session integration.");

            var gameListener = _sentryMonoBehaviourGenerator();
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
