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
        _sentryMonoBehaviour.ApplicationResuming += () =>
        {
            options.DiagnosticLogger?.LogDebug("Application resumed. Creating new Trace.");
            hub.ConfigureScope(UpdatePropagationContext);
        };

        if (options is not SentryUnityOptions unityOptions || unityOptions.TracesSampleRate > 0)
        {
            return;
        }

        // The generated trace context would immediately get overwritten by the StartupTracingIntegration
        if (!unityOptions.AutoStartupTraces)
        {
            hub.ConfigureScope(UpdatePropagationContext);
        }

        // The generated trace context would immediately get overwritten by the SceneManagerTracingIntegration
        if (!unityOptions.AutoSceneLoadTraces)
        {
            _sceneManager.ActiveSceneChanged += (_, _) =>
            {
                options.DiagnosticLogger?.LogDebug("Active Scene changed. Creating new Trace.");
                hub.ConfigureScope(UpdatePropagationContext);
            };
        }
    }

    private static void UpdatePropagationContext(Scope scope) =>
        scope.SetPropagationContext(new SentryPropagationContext());
}
