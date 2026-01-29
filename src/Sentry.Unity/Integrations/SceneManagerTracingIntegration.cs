using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Internal;
using UnityEngine.SceneManagement;

namespace Sentry.Unity;

internal class SceneManagerTracingIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        // This should never happen, but if it does...
        var options = sentryOptions as SentryUnityOptions ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        if (options is { TracesSampleRate: > 0.0f, AutoSceneLoadTraces: true })
        {
            if (SceneManagerAPI.overrideAPI != null)
            {
                // TODO: Add a place to put a custom 'SceneManagerAPI' on the editor window so we can "decorate" it.
                options.LogWarning("Registering '{0}' integration - overwriting the previous SceneManagerAPI.overrideAPI.", nameof(SceneManagerTracingIntegration));
            }

            SceneManagerAPI.overrideAPI = new SceneManagerTracingAPI(options.DiagnosticLogger);
        }
        else
        {
            options.LogDebug("Skipping registering '{0}'.  Either 'TracesSampleRate' set to '0' or 'AutoSceneLoadTraces' is set to 'false'", nameof(SceneManagerTracingIntegration));
        }
    }
}

public class SceneManagerTracingAPI : SceneManagerAPI
{
    public const string TransactionOperation = "scene.load";
    private const string SpanOperation = "scene.load";
    private const string SceneLoadOrigin = "auto.navigation.unity";
    private readonly IDiagnosticLogger? _logger;

    public SceneManagerTracingAPI(IDiagnosticLogger? logger) =>
        _logger = logger;

    protected override UnityEngine.AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
    {
        _logger?.LogInfo("Creating '{0}' transaction for '{1}'.", TransactionOperation, sceneName);

        var transaction = SentrySdk.StartTransaction("scene.loading", TransactionOperation);
        transaction.SetOrigin(SceneLoadOrigin);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        _logger?.LogDebug("Creating '{0}' span.", SpanOperation);
        var span = SentrySdk.GetSpan()?.StartChild(SpanOperation, sceneName ?? $"buildIndex:{sceneBuildIndex}");
        span?.SetOrigin(SceneLoadOrigin);

        var asyncOp = base.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);

        // TODO: setExtra()? e.g. from the LoadSceneParameters:
        // https://github.com/Unity-Technologies/UnityCsReference/blob/02d565cf3dd0f6b15069ba976064c75dc2705b08/Runtime/Export/SceneManager/SceneManager.cs#L30
        // Note: asyncOp.completed triggers in the next frame after finishing (so the time isn't precise).
        // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/AsyncOperation-completed.html
        asyncOp.completed += _ =>
        {
            _logger?.LogInfo("Finishing '{0}' transaction for '{1}'.", TransactionOperation, sceneName);

            span?.Finish(SpanStatus.Ok);
            transaction.Finish(SpanStatus.Ok);
        };

        return asyncOp;
    }
}
