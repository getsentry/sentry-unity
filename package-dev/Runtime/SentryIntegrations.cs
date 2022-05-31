#if UNITY_2020 || UNITY_2021 || UNITY_2022
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using System;
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
            if (options.TracesSampleRate > 0.0) {
                options.AddIntegration(new SceneManagerTracingIntegration());
            } else {
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
            if (SceneManagerAPI.overrideAPI is not null)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Warning,
                    "Registering SceneManagerTracing integration - overwriting the previouse SceneManagerAPI.overrideAPI.");
            }

            SceneManagerAPI.overrideAPI = new SceneManagerTracingAPI();
        }

        internal class SceneManagerTracingAPI : SceneManagerAPI
        {
            protected override AsyncOperation LoadSceneAsyncByNameOrIndex(string? sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
            {
                var transaction = SentrySdk.StartTransaction("load-scene", (sceneName is null) ? $"buildIndex:{sceneBuildIndex}" : sceneName);
                var asyncOp = base.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);

                if (transaction.IsSampled is true)
                {
                    // TODO setExtra()? e.g. from the LoadSceneParameters:
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/02d565cf3dd0f6b15069ba976064c75dc2705b08/Runtime/Export/SceneManager/SceneManager.cs#L30
                    // Note: asyncOp.completed triggers in the next frame after finishing (so the time isn't precise).
                    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/AsyncOperation-completed.html
                    asyncOp.completed += (_) => transaction.Finish();
                }

                return asyncOp;
            }
        }
    }
#endif
}
