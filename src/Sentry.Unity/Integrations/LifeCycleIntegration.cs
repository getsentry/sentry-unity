using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

internal class LifeCycleIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _sentryMonoBehaviour;
    private readonly IApplication _application;

    public LifeCycleIntegration(SentryMonoBehaviour sentryMonoBehaviour, IApplication? application = null)
    {
        _sentryMonoBehaviour = sentryMonoBehaviour;
        _application = application ?? ApplicationAdapter.Instance;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        if (!options.AutoSessionTracking)
        {
            return;
        }

        _sentryMonoBehaviour.ApplicationResuming += () =>
        {
            if (hub.IsSessionActive)
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
            if (hub.IsSessionActive)
            {
                options.DiagnosticLogger?.LogDebug("Pausing session.");
                hub.PauseSession();
            }
            // else
            // {
            //     // The SDK captured an unhandled exception and automatically ended the session
            // }
        };

        _application.Quitting += () => hub.EndSession();
    }
}
