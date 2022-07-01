#if UNITY_2020_1_OR_NEWER
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using System;
using System.Threading.Tasks;
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
        public void Register(IHub hub, SentryOptions options)
        {
            if (SceneManagerAPI.overrideAPI != null)
            {
                // TODO: Add a place to put a custom 'SceneManagerAPI' on the window so we can "decorate" it.
                options.DiagnosticLogger?.Log(SentryLevel.Warning,
                    "Registering SceneManagerTracing integration - overwriting the previous SceneManagerAPI.overrideAPI.");
            }

            SceneManagerAPI.overrideAPI = new SceneManagerTracingAPI();
        }

        internal class SceneManagerTracingAPI : SceneManagerAPI
        {
            protected override AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
            {
                var transaction = SentrySdk.StartTransaction("idle-transaction-from-scene-load", sceneName ?? $"buildIndex:{sceneBuildIndex}");
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
                    span.Finish(SpanStatus.Ok);
                };

                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    Debug.Log("Finishing transaction");
                    transaction.Finish(SpanStatus.Ok);
                }).ConfigureAwait(false);

                return asyncOp;
            }
        }
    }
#endif
}
