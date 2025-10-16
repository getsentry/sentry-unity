using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

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
            if (Sentry.SentrySdk.IsSessionActive)
            {
                options.DiagnosticLogger?.LogDebug("Resuming session.");
                hub.ResumeSession();
            }
            else
            {
                options.DiagnosticLogger?.LogDebug("No active session to resume found. Starting a new session.");
                hub.StartSession();
            }
        };
        _sentryMonoBehaviour.ApplicationPausing += () =>
        {
            if (Sentry.SentrySdk.IsSessionActive)
            {
                options.DiagnosticLogger?.LogDebug("Pausing session.");
                hub.PauseSession();
            }
            else
            {
                // TODO: Do we want to log that there was no active session to pause?
                // I.e. the SDK captured an unhandled exception and automatically ended the session
            }
        };
    }
}
