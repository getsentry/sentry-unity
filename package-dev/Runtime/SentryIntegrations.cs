#if !UNITY_EDITOR
#if UNITY_WEBGL
#define SENTRY_WEBGL
#endif
#endif

using Sentry.Extensibility;
using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    internal static class SentryStartupTracing
    {
        private static StartupTracingIntegration StartupTracingIntegration;

        public static void SetUpTracingIntration(SentryUnityOptions options)
        {
            if (options.TracesSampleRate > 0.0)
            {
// On WebGL the SDK initializes on BeforeScene so the Startup Tracing won't work properly. https://github.com/getsentry/sentry-unity/issues/1000
#if !SENTRY_WEBGL
                if (options.AutoStartupTraces)
                {
                    StartupTracingIntegration = new StartupTracingIntegration();
                    options.AddIntegration(StartupTracingIntegration);
                }
#endif
            }
        }

        public static void StartTracing()
        {
            if (StartupTracingIntegration != null)
            {
                StartupTracingIntegration.StartTracing();
            }
        }

    }

#if !SENTRY_WEBGL
    internal class StartupTracingIntegration : ISdkIntegration
    {
        private const string StartupTransactionOperation = "app.start";
        private static ISpan InitSpan;
        private const string InitSpanOperation = "runtime.init";
        public static ISpan SubSystemRegistrationSpan;
        private const string SubSystemSpanOperation = "runtime.init.subsystem";
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

        public static void StartTracing()
        {
            if (!IntegrationRegistered || StartupAlreadyCaptured)
            {
                return;
            }

            Logger.LogInfo("Creating '{0}' transaction for runtime initialization.",
                StartupTransactionOperation);

            var runtimeStartTransaction =
                SentrySdk.StartTransaction("runtime.initialization", StartupTransactionOperation);
            SentrySdk.ConfigureScope(scope => scope.Transaction = runtimeStartTransaction);

            Logger.LogDebug("Creating '{0}' span.", InitSpanOperation);
            InitSpan = runtimeStartTransaction.StartChild(InitSpanOperation, "runtime initialization");
            Logger.LogDebug("Creating '{0}' span.", SubSystemSpanOperation);
            SubSystemRegistrationSpan = InitSpan.StartChild(SubSystemSpanOperation, "subsystem registration");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void AfterAssembliesLoaded()
        {
            if (!IntegrationRegistered || StartupAlreadyCaptured)
            {
                return;
            }

            SubSystemRegistrationSpan?.Finish(SpanStatus.Ok);
            SubSystemRegistrationSpan = null;

            Logger?.LogDebug("Creating '{0}' span.", AfterAssembliesSpanOperation);
            AfterAssembliesSpan = InitSpan?.StartChild(AfterAssembliesSpanOperation, "after assemblies");
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
            SplashScreenSpan = InitSpan?.StartChild(SplashScreenSpanOperation, "splashscreen");
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
            FirstSceneLoadSpan = InitSpan?.StartChild(FirstSceneLoadSpanOperation, "first scene load");
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

            InitSpan?.Finish(SpanStatus.Ok);
            InitSpan = null;

            Logger?.LogInfo("Finishing '{0}' transaction.", StartupTransactionOperation);
            SentrySdk.ConfigureScope(s =>
            {
                s.Transaction?.Finish(SpanStatus.Ok);
            });

            StartupAlreadyCaptured = true;
        }
    }
#endif
}
