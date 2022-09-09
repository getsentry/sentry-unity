#if UNITY_2020_3_OR_NEWER
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

using JetBrains.Annotations;
using Sentry.Extensibility;
using Sentry.Integrations;
using UnityEngine;
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
    public class SceneManagerTracingIntegration : ISdkIntegration
    {
        [CanBeNull] private static ISpan AfterAssembliesSpan;
        private const string AfterAssembliesSpanName = "after.assemblies";
        [CanBeNull] private static ISpan SplashScreenSpan;
        private const string SplashScreenSpanName = "splashscreen";
        [CanBeNull] private static ISpan FirstSceneLoadSpan;
        private const string FirstSceneLoadSpanName = "first.scene.load";

        // Flag to make sure we create spans through the runtime initialization only once
        private static bool ShouldCreateSpans = true;

        [CanBeNull] private static IDiagnosticLogger Logger;

        public void Register(IHub hub, SentryOptions options)
        {
            Logger = options.DiagnosticLogger;

            if (SceneManagerAPI.overrideAPI != null)
            {
                // TODO: Add a place to put a custom 'SceneManagerAPI' on the editor window so we can "decorate" it.
                Logger?.Log(SentryLevel.Warning,
                    "Registering SceneManagerTracing integration - overwriting the previous SceneManagerAPI.overrideAPI.");
            }

            SceneManagerAPI.overrideAPI = new SceneManagerTracingAPI(Logger);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void AfterAssembliesLoaded()
        {
            if (!ShouldCreateSpans)
            {
                return;
            }

            SentryInitialization.AssembliesLoadSpan?.Finish(SpanStatus.Ok);
            SentryInitialization.AssembliesLoadSpan = null;

            Logger?.LogDebug("Creating '{0}' span.", AfterAssembliesSpanName);
            AfterAssembliesSpan = SentryInitialization.InitializationSpan?.StartChild(AfterAssembliesSpanName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void BeforeSplashScreen()
        {
            if (!ShouldCreateSpans)
            {
                return;
            }

            AfterAssembliesSpan?.Finish(SpanStatus.Ok);
            AfterAssembliesSpan = null;

            Logger?.LogDebug("Creating '{0}' span.", SplashScreenSpanName);
            SplashScreenSpan = SentryInitialization.InitializationSpan?.StartChild(SplashScreenSpanName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BeforeSceneLoad()
        {
            if (!ShouldCreateSpans)
            {
                return;
            }

            SplashScreenSpan?.Finish(SpanStatus.Ok);
            SplashScreenSpan = null;

            Logger?.LogDebug("Creating '{0}' span.", FirstSceneLoadSpanName);
            FirstSceneLoadSpan = SentryInitialization.InitializationSpan?.StartChild(FirstSceneLoadSpanName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AfterSceneLoad()
        {
            FirstSceneLoadSpan?.Finish(SpanStatus.Ok);
            FirstSceneLoadSpan = null;

            SentryInitialization.InitializationSpan?.Finish(SpanStatus.Ok);
            SentryInitialization.InitializationSpan = null;

            Logger?.LogDebug("Finishing '{0}' transaction.", SentryInitialization.StartupTransactionName);
            SentrySdk.ConfigureScope(s =>
            {
                s.Transaction?.Finish(SpanStatus.Ok);
            });

            ShouldCreateSpans = false;
        }
    }

    public class SceneManagerTracingAPI : SceneManagerAPI
    {
        public const string TransactionName = "scene.loading";
        private const string SpanName = "scene.load";
        [CanBeNull] private readonly IDiagnosticLogger _logger;

        public SceneManagerTracingAPI([CanBeNull] IDiagnosticLogger logger)
        {
            _logger = logger;
        }

        protected override AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
        {
            _logger?.LogInfo("Creating '{0}' span for '{1}'.", SpanName, sceneName);

            var transaction = SentrySdk.StartTransaction(TransactionName, sceneName ?? $"buildIndex:{sceneBuildIndex}");
            SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
            var span = SentrySdk.GetSpan()?.StartChild(SpanName);

            var asyncOp = base.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);

            // TODO: setExtra()? e.g. from the LoadSceneParameters:
            // https://github.com/Unity-Technologies/UnityCsReference/blob/02d565cf3dd0f6b15069ba976064c75dc2705b08/Runtime/Export/SceneManager/SceneManager.cs#L30
            // Note: asyncOp.completed triggers in the next frame after finishing (so the time isn't precise).
            // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/AsyncOperation-completed.html
            asyncOp.completed += _ =>
            {
                _logger?.LogInfo("Finishing '{0}' transaction for '{1}'.", TransactionName, sceneName);

                span?.Finish(SpanStatus.Ok);
                transaction.Finish(SpanStatus.Ok);
            };

            return asyncOp;
        }
    }
#endif
}
