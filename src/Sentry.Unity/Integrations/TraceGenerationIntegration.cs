using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

internal sealed class TraceGenerationIntegration : ISdkIntegration
{
    private readonly ISceneManager _sceneManager;
    private readonly ISentryMonoBehaviour _sentryMonoBehaviour;

    public TraceGenerationIntegration(SentryMonoBehaviour sentryMonoBehaviour) : this(sentryMonoBehaviour, SceneManagerAdapter.Instance)
    { }

    internal TraceGenerationIntegration(ISentryMonoBehaviour sentryMonoBehaviour, ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
        _sentryMonoBehaviour = sentryMonoBehaviour;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        hub.ConfigureScope(UpdatePropagationContext);

        _sentryMonoBehaviour.ApplicationResuming += () =>
        {
            options.DiagnosticLogger?.LogDebug("Application resumed. Creating new Trace.");
            hub.ConfigureScope(UpdatePropagationContext);
        };

        _sceneManager.ActiveSceneChanged += (_, _) =>
        {
            options.DiagnosticLogger?.LogDebug("Active Scene changed. Creating new Trace.");
            hub.ConfigureScope(UpdatePropagationContext);
        };
    }

    private static void UpdatePropagationContext(Scope scope) =>
        scope.SetPropagationContext(new SentryPropagationContext());
}
