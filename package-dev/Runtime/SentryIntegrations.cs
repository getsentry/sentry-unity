#if UNITY_2020_3_OR_NEWER
#define SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
#endif

#if !UNITY_EDITOR
#if UNITY_WEBGL
#define SENTRY_WEBGL
#endif
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
            if (options.TracesSampleRate > 0.0)
            {
// On WebGL the SDK initializes on BeforeScene so the Startup Tracing won't work properly. https://github.com/getsentry/sentry-unity/issues/1000
#if !SENTRY_WEBGL
                if (options.AutoStartupTraces)
                {
                    options.AddIntegration(new StartupTracingIntegration());
                }
#endif
#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
                if (options.AutoSceneLoadTraces)
                {
                    options.AddIntegration(new SceneManagerTracingIntegration());
                }
#endif
            }
            else
            {
#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
                options.DiagnosticLogger?.LogDebug("Skipping SceneManagerTracing integration because performance tracing is disabled.");
#endif
            }

        }
    }

#if !SENTRY_WEBGL
    public class StartupTracingIntegration : ISdkIntegration
    {
        private static ISpan AfterAssembliesSpan;
        private const string AfterAssembliesSpanOperation = "runtime.init.afterassemblies";
        private static ISpan SplashScreenSpan;
        private const string SplashScreenSpanOperation = "runtime.init.splashscreen";
        private static ISpan FirstSceneLoadSpan;
        private const string FirstSceneLoadSpanOperation = "runtime.init.firstscene";

        // Flag to make sure we create spans through the runtime initialization only once
        private static bool StartupAlreadyCaptured;
        private static bool IntegrationRegistered;

        private static IDiagnosticLogger Logger;

        public void Register(IHub hub, SentryOptions options)
        {
            Logger = options.DiagnosticLogger;
            IntegrationRegistered = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void AfterAssembliesLoaded()
        {
            if (!IntegrationRegistered || StartupAlreadyCaptured)
            {
                return;
            }

            SentryInitialization.SubSystemRegistrationSpan?.Finish(SpanStatus.Ok);
            SentryInitialization.SubSystemRegistrationSpan = null;

            Logger?.LogDebug("Creating '{0}' span.", AfterAssembliesSpanOperation);
            AfterAssembliesSpan = SentryInitialization.InitSpan?.StartChild(AfterAssembliesSpanOperation, "after assemblies");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void BeforeSplashScreen()
        {
            if (!IntegrationRegistered || StartupAlreadyCaptured)
            {
                return;
            }

            AfterAssembliesSpan?.Finish(SpanStatus.Ok);
            AfterAssembliesSpan = null;

            Logger?.LogDebug("Creating '{0}' span.", SplashScreenSpanOperation);
            SplashScreenSpan = SentryInitialization.InitSpan?.StartChild(SplashScreenSpanOperation, "splashscreen");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BeforeSceneLoad()
        {
            if (!IntegrationRegistered || StartupAlreadyCaptured)
            {
                return;
            }

            SplashScreenSpan?.Finish(SpanStatus.Ok);
            SplashScreenSpan = null;

            Logger?.LogDebug("Creating '{0}' span.", FirstSceneLoadSpanOperation);
            FirstSceneLoadSpan = SentryInitialization.InitSpan?.StartChild(FirstSceneLoadSpanOperation, "first scene load");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AfterSceneLoad()
        {
            if (!IntegrationRegistered || StartupAlreadyCaptured)
            {
                return;
            }

            FirstSceneLoadSpan?.Finish(SpanStatus.Ok);
            FirstSceneLoadSpan = null;

            SentryInitialization.InitSpan?.Finish(SpanStatus.Ok);
            SentryInitialization.InitSpan = null;

            Logger?.LogInfo("Finishing '{0}' transaction.", SentryInitialization.StartupTransactionOperation);
            SentrySdk.ConfigureScope(s =>
            {
                s.Transaction?.Finish(SpanStatus.Ok);
            });

            StartupAlreadyCaptured = true;
        }
    }
#endif

#if SENTRY_SCENE_MANAGER_TRACING_INTEGRATION
    public class SceneManagerTracingIntegration : ISdkIntegration
    {
        private static IDiagnosticLogger Logger;

        public void Register(IHub hub, SentryOptions options)
        {
            Logger = options.DiagnosticLogger;

            if (SceneManagerAPI.overrideAPI != null)
            {
                // TODO: Add a place to put a custom 'SceneManagerAPI' on the editor window so we can "decorate" it.
                Logger?.LogWarning("Registering SceneManagerTracing integration - overwriting the previous SceneManagerAPI.overrideAPI.");
            }

            SceneManagerAPI.overrideAPI = new SceneManagerTracingAPI(Logger);
        }
    }

    public class SceneManagerTracingAPI : SceneManagerAPI
    {
        public const string TransactionOperation = "scene.load";
        private const string SpanOperation = "scene.load";
        private readonly IDiagnosticLogger _logger;

        public SceneManagerTracingAPI(IDiagnosticLogger logger)
        {
            _logger = logger;
        }

        protected override AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
        {
            _logger?.LogInfo("Creating '{0}' transaction for '{1}'.", TransactionOperation, sceneName);

            var transaction = SentrySdk.StartTransaction("scene.loading", TransactionOperation);
            SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

            _logger?.LogDebug("Creating '{0}' span.", SpanOperation);
            var span = SentrySdk.GetSpan()?.StartChild(SpanOperation, sceneName ?? $"buildIndex:{sceneBuildIndex}");

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
#endif
}
