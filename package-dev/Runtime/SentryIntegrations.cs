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
        private static ITransaction StartupTransaction;
        private static ISpan SplashAndAssembliesSpan;
        private static ISpan SceneLoadSpan;

        private static bool ShouldCaptureStartup;

        private IDiagnosticLogger _logger;

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
            ShouldCaptureStartup = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void SubsystemRegistration()
        {
            // Sentry inits here
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void BeforeSplashScreen()
        {
            if (!ShouldCaptureStartup)
            {
                return;
            }

            StartupTransaction = SentrySdk.StartTransaction("transaction-from-startup", "Splash Screen");
            SentrySdk.ConfigureScope(scope => scope.Transaction = StartupTransaction);

            SplashAndAssembliesSpan = SentrySdk.GetSpan()?.StartChild("startup.splashscreen");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void AfterAssembliesLoaded()
        {
            // There is not really anything here for use to measure/capture?
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BeforeSceneLoad()
        {
            if (!ShouldCaptureStartup)
            {
                return;
            }

            SplashAndAssembliesSpan.Finish(SpanStatus.Ok);
            SceneLoadSpan = SentrySdk.GetSpan()?.StartChild("startup.scene.load");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AfterSceneLoad()
        {
            if (!ShouldCaptureStartup)
            {
                return;
            }

            SceneLoadSpan.Finish(SpanStatus.Ok);
            StartupTransaction.Finish(SpanStatus.Ok);
        }
    }

    public class SceneManagerTracingAPI : SceneManagerAPI
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
                _logger.LogInfo("Finishing scene load transaction for '{0}'.", sceneName);

                span?.Finish(SpanStatus.Ok);
                transaction.Finish(SpanStatus.Ok);
            };

            return asyncOp;
        }
    }
#endif
}
