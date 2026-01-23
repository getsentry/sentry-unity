using System;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

/// <summary>
/// The TraceGenerationIntegration is used to generate new trace propagation contexts
/// /// </summary>
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

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        // This should never happen, but if it does...
        var options = sentryOptions as SentryUnityOptions ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        var isTracingEnabled = options.TracesSampleRate > 0.0f;

        // Create initial trace context if tracing is disabled or startup tracing is disabled
        if (!isTracingEnabled || !options.AutoStartupTraces)
        {
            options.DiagnosticLogger?.LogDebug("Startup. Creating new Trace.");
            hub.ConfigureScope(scope => scope.SetPropagationContext(new SentryPropagationContext()));
        }
    }
}
