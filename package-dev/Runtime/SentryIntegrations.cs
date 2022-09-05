#if true
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Integrations;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    public static class SentryIntegrations
    {
        public static void Configure(SentryUnityOptions options)
        {
#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
            if (options.TracesSampleRate > 0.0)
            {
                options.AddIntegration(new SceneManagerTracingIntegration());
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
        private SentryOptions _options;

        private ISpan _autoInitSpan;
        private ITransaction _autoInitTransaction;

        public void Register(IHub hub, SentryOptions options)
        {
            _options = options;

            if (SceneManagerAPI.overrideAPI != null)
            {
                // TODO: Add a place to put a custom 'SceneManagerAPI' on the editor window so we can "decorate" it.
                options.DiagnosticLogger?.Log(SentryLevel.Warning,
                    "Registering SceneManagerTracing integration - overwriting the previous SceneManagerAPI.overrideAPI.");
            }

            SceneManagerAPI.overrideAPI = new SceneManagerTracingAPI();

            // We have to start the transaction manually to capture the initial scene loading during startup
            if (SentryInitialization.IsAutoInitializing)
            {
                options.DiagnosticLogger?.LogDebug("Sentry is self-initializing. Starting startup scene load transaction.");

                SceneManager.sceneLoaded += EndInitTransaction;

                _autoInitTransaction = SentrySdk.StartTransaction("transaction-from-startup-scene-load", $"Loading '{SceneManager.GetSceneAt(0).name}'");
                SentrySdk.ConfigureScope(scope => scope.Transaction = _autoInitTransaction);

                _autoInitSpan = SentrySdk.GetSpan()?.StartChild("scene.load");
            }
        }

        private void EndInitTransaction(Scene scene, LoadSceneMode mode)
        {
            _options.DiagnosticLogger?.LogDebug("Ending startup scene load transaction.");

            _autoInitSpan?.Finish(SpanStatus.Ok);
            _autoInitTransaction.Finish(SpanStatus.Ok);

            // Only run once
            SceneManager.sceneLoaded -= EndInitTransaction;
        }

        internal class SceneManagerTracingAPI : SceneManagerAPI
        {
            protected override AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
            {
                Debug.Log("Scene load start");

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
                    Debug.Log("Scene load finished");
                    span?.Finish(SpanStatus.Ok);
                    transaction.Finish(SpanStatus.Ok);
                };

                // Task.Run(async () =>
                // {
                //     await Task.Delay(3000);
                //     Debug.Log("Finishing transaction");
                //     transaction.Finish(SpanStatus.Ok);
                // }).ConfigureAwait(false);

                return asyncOp;
            }
        }
    }
#endif
}
