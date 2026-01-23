using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Internal;
using UnityEngine;

namespace Sentry.Unity.Integrations;

internal class StartupTracingIntegration : ISdkIntegration
{
    private const string StartupTransactionOperation = "app.start";
    private const string StartupTransactionOrigin = "auto.app.start";
    private const string StartupSpanOrigin = "auto.app.start.unity";
    internal static ISpan? InitSpan;
    private const string InitSpanOperation = "runtime.init";
    internal static ISpan? SubSystemRegistrationSpan;
    private const string SubSystemSpanOperation = "runtime.init.subsystem";
    internal static ISpan? AfterAssembliesSpan;
    private const string AfterAssembliesSpanOperation = "runtime.init.afterassemblies";
    internal static ISpan? SplashScreenSpan;
    private const string SplashScreenSpanOperation = "runtime.init.splashscreen";
    internal static ISpan? FirstSceneLoadSpan;
    private const string FirstSceneLoadSpanOperation = "runtime.init.firstscene";

    internal static bool IsGameStartupFinished; // Flag to make sure we only create spans during the game's startup.
    internal static bool IsIntegrationRegistered;

    private static IDiagnosticLogger? Logger;

    // For testing. Methods with the RuntimeLoad attribute cannot have arguments
    internal static IApplication? Application = null;

    public void Register(IHub hub, SentryOptions options)
    {
        Logger = options.DiagnosticLogger;

        // This should never happen, but if it does...
        var options = sentryOptions as SentryUnityOptions ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        if (options is { TracesSampleRate: > 0, AutoStartupTraces: true })
        {
            IsIntegrationRegistered = true;
        }
        else if (options is SentryUnityOptions unityOptions)
        {
            options.LogDebug("Skipping registering '{0}'.  Either 'TracesSampleRate' set to '0' or 'AutoStartupTraces' is set to 'false'", nameof(StartupTracingIntegration));
        }
    }

    internal static bool IsStartupTracingAllowed()
    {
        Application ??= ApplicationAdapter.Instance;
        if (!Application.IsEditor
            && Application.Platform != RuntimePlatform.WebGLPlayer // Startup Tracing does not properly work on WebGL
            && IsIntegrationRegistered
            && !IsGameStartupFinished)
        {
            return true;
        }

        return false;
    }

    public static void StartTracing()
    {
        if (!IsStartupTracingAllowed())
        {
            return;
        }

        Logger?.LogInfo("Creating '{0}' transaction for runtime initialization.",
            StartupTransactionOperation);

        var runtimeStartTransaction =
            SentrySdk.StartTransaction("runtime.initialization", StartupTransactionOperation);
        runtimeStartTransaction.SetOrigin(StartupTransactionOrigin);
        SentrySdk.ConfigureScope(scope => scope.Transaction = runtimeStartTransaction);

        Logger?.LogDebug("Creating '{0}' span.", InitSpanOperation);
        InitSpan = runtimeStartTransaction.StartChild(InitSpanOperation, "runtime initialization");
        InitSpan.SetOrigin(StartupSpanOrigin);
        Logger?.LogDebug("Creating '{0}' span.", SubSystemSpanOperation);
        SubSystemRegistrationSpan = InitSpan.StartChild(SubSystemSpanOperation, "subsystem registration");
        SubSystemRegistrationSpan.SetOrigin(StartupSpanOrigin);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void AfterAssembliesLoaded()
    {
        if (!IsStartupTracingAllowed())
        {
            return;
        }

        SubSystemRegistrationSpan?.Finish(SpanStatus.Ok);
        SubSystemRegistrationSpan = null;

        Logger?.LogDebug("Creating '{0}' span.", AfterAssembliesSpanOperation);
        AfterAssembliesSpan = InitSpan?.StartChild(AfterAssembliesSpanOperation, "after assemblies");
        AfterAssembliesSpan?.SetOrigin(StartupSpanOrigin);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public static void BeforeSplashScreen()
    {
        if (!IsStartupTracingAllowed())
        {
            return;
        }

        AfterAssembliesSpan?.Finish(SpanStatus.Ok);
        AfterAssembliesSpan = null;

        Logger?.LogDebug("Creating '{0}' span.", SplashScreenSpanOperation);
        SplashScreenSpan = InitSpan?.StartChild(SplashScreenSpanOperation, "splashscreen");
        SplashScreenSpan?.SetOrigin(StartupSpanOrigin);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void BeforeSceneLoad()
    {
        if (!IsStartupTracingAllowed())
        {
            return;
        }

        SplashScreenSpan?.Finish(SpanStatus.Ok);
        SplashScreenSpan = null;

        Logger?.LogDebug("Creating '{0}' span.", FirstSceneLoadSpanOperation);
        FirstSceneLoadSpan = InitSpan?.StartChild(FirstSceneLoadSpanOperation, "first scene load");
        FirstSceneLoadSpan?.SetOrigin(StartupSpanOrigin);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AfterSceneLoad()
    {
        if (!IsStartupTracingAllowed())
        {
            // To make sure late init calls don't try to trace the startup
            IsGameStartupFinished = true;
            return;
        }

        FirstSceneLoadSpan?.Finish(SpanStatus.Ok);
        FirstSceneLoadSpan = null;

        InitSpan?.Finish(SpanStatus.Ok);
        InitSpan = null;

        Logger?.LogInfo("Finishing '{0}' transaction.", StartupTransactionOperation);
        SentrySdk.ConfigureScope(s => s.Transaction?.Finish(SpanStatus.Ok));

        IsGameStartupFinished = true;
    }
}
