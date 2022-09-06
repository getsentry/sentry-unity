#if UNITY_2020_3_OR_NEWER
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using Sentry.Extensibility;
using Sentry.Integrations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    public static class SentryIntegrations
    {
        public static void Configure(SentryUnityOptions options, bool isSelfInitializing = false)
        {
#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
            if (options.TracesSampleRate > 0.0)
            {
                options.AddIntegration(new SceneManagerTracingIntegration(isSelfInitializing));
            }
            else
            {
                options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Skipping SceneManagerTracing integration because performance tracing is disabled.");
            }
#endif
        }
    }

#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
    internal class SceneManagerTracingIntegration : ISdkIntegration
    {
        private IDiagnosticLogger _logger;

        private readonly bool _isSelfInitializing;
        private ISpan _startupSpan;
        private ITransaction _startupTransaction;

        public SceneManagerTracingIntegration(bool isSelfInitializing)
        {
            _isSelfInitializing = isSelfInitializing;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            _logger = options.DiagnosticLogger;

            if (SceneManagerAPI.overrideAPI != null)
            {
                // TODO: Add a place to put a custom 'SceneManagerAPI' on the editor window so we can "decorate" it.
                _logger?.Log(SentryLevel.Warning,
                    "Registering SceneManagerTracing integration - overwriting the previous SceneManagerAPI.overrideAPI.");
            }

            SceneManagerAPI.overrideAPI = new SceneManagerTracingAPI(_logger);

            // In case of self-initialization during startup there is no callback for the load event. We have to start the
            // transaction manually to capture the initial scene loading.
            if (_isSelfInitializing)
            {
                _logger?.LogInfo("Sentry self initialized. Starting startup scene load transaction.");

                _startupTransaction = SentrySdk.StartTransaction("transaction-from-startup-scene-load", $"Loading '{SceneManager.GetSceneAt(0).name}'");
                SentrySdk.ConfigureScope(scope => scope.Transaction = _startupTransaction);

                _startupSpan = SentrySdk.GetSpan()?.StartChild("scene.load");

                SceneManager.sceneLoaded += EndStartupSceneLoadTransaction;
            }
        }

        private void EndStartupSceneLoadTransaction(Scene scene, LoadSceneMode mode)
        {
            _logger?.LogInfo("Finishing startup scene load transaction.");

            _startupSpan?.Finish(SpanStatus.Ok);
            _startupTransaction.Finish(SpanStatus.Ok);

            // Only run once - Letting the SceneManagerAPI take over
            SceneManager.sceneLoaded -= EndStartupSceneLoadTransaction;
        }

        internal class SceneManagerTracingAPI : SceneManagerAPI
        {
            private readonly IDiagnosticLogger _logger;

            public SceneManagerTracingAPI(IDiagnosticLogger logger)
            {
                _logger = logger;
            }

            protected override AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
            {
                _logger.LogInfo("Starting scene load transaction for '{0}'.", sceneName);

                var transaction = SentrySdk.StartTransaction("transaction-from-scene-load", sceneName ?? $"buildIndex:{sceneBuildIndex}");
                SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

                var span = SentrySdk.GetSpan()?.StartChild("scene.load");
                var asyncOp = base.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);

                // TODO: setExtra()? e.g. from the LoadSceneParameters:
                // https://github.com/Unity-Technologies/UnityCsReference/blob/02d565cf3dd0f6b15069ba976064c75dc2705b08/Runtime/Export/SceneManager/SceneManager.cs#L30
                // Note: asyncOp.completed triggers in the next frame after finishing (so the time isn't precise).
                // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/AsyncOperation-completed.html
                asyncOp.completed += (_) =>
                {
                    _logger.LogInfo("Finishing scene load transaction for {0}.", sceneName);

                    span?.Finish(SpanStatus.Ok);
                    transaction.Finish(SpanStatus.Ok);
                };

                return asyncOp;
            }
        }
    }
#endif
}
