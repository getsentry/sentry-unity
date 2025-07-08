#if !UNITY_EDITOR
#if UNITY_WEBGL
#define SENTRY_WEBGL
#endif
#endif

using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Unity;
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
}
