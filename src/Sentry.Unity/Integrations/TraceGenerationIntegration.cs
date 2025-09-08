using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

/// <summary>
/// The TraceGenerationIntegration is used in case of
/// </summary>
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
            options.DiagnosticLogger?.LogDebug("Game resuming. Creating new Trace.");
            hub.ConfigureScope(scope => scope.SetPropagationContext(new SentryPropagationContext()));;
        };

        if (options is not SentryUnityOptions unityOptions)
        {
            return;
        }

        var isTracingEnabled = unityOptions.TracesSampleRate > 0.0f;

        // Create initial trace context if tracing is disabled or startup tracing is disabled
        if (!isTracingEnabled || !unityOptions.AutoStartupTraces)
        {
            options.DiagnosticLogger?.LogDebug("Startup. Creating new Trace.");
            hub.ConfigureScope(scope => scope.SetPropagationContext(new SentryPropagationContext()));
        }

        // Set up scene change handling if tracing is disabled or auto scene load traces are disabled
        if (!isTracingEnabled || !unityOptions.AutoSceneLoadTraces)
        {
            _sceneManager.ActiveSceneChanged += (_, _) =>
            {
                options.DiagnosticLogger?.LogDebug("Active Scene changed. Creating new Trace.");
                hub.ConfigureScope(scope => scope.SetPropagationContext(new SentryPropagationContext()));
            };
        }
    }
}
