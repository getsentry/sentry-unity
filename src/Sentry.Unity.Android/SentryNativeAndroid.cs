using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using Sentry.Unity.NativeUtils;
using UnityEngine;

namespace Sentry.Unity.Android;

/// <summary>
/// Access to the Sentry native support on Android.
/// </summary>
public static class SentryNativeAndroid
{
    // This is an internal static field that gets overwritten during testing. We cannot have it as optional
    // parameter on `Configure` due SentryNativeAndroid being public
    internal static ISentryJava? SentryJava;

    private static IDiagnosticLogger? Logger;

    /// <summary>
    /// Configures the native Android support.
    /// </summary>
    /// <param name="options">The Sentry Unity options to use.</param>
    public static void Configure(SentryUnityOptions options)
    {
        Logger = options.DiagnosticLogger;

        Logger?.LogInfo("Attempting to configure native support via the Android SDK");

        if (!options.AndroidNativeSupportEnabled)
        {
            Logger?.LogDebug("Native support is disabled for Android");
            return;
        }

        Logger?.LogDebug("Checking whether the Android SDK is present.");

        var isExternal = options.AndroidNativeInitializationType is NativeInitializationType.External;

        // If it's not been set (in a test)
        SentryJava ??= new SentryJava(Logger);
        if (!SentryJava.IsSentryJavaPresent())
        {
            if (isExternal)
            {
                Logger?.LogError("The Android SDK is missing. With external initialization, the app has to depend on " +
                                 "'io.sentry:sentry-android'.");
            }
            else
            {
                Logger?.LogError("Android Native Support has been enabled but the " +
                                 "Android SDK is missing. This could have been caused by a mismatching" +
                                 "build time / runtime configuration. Please make sure you have " +
                                 "Android Native Support enabled during build time.");
            }
            return;
        }

        Logger?.LogDebug("Checking whether the Android SDK has already been initialized");

        var isAttached = false;

        if (SentryJava.IsEnabled() is true)
        {
            Logger?.LogDebug("The Android SDK is already initialized");
            isAttached = isExternal;
        }
        else if (isExternal && SentryJava.IsMainProcess() is not false)
        {
            Logger?.LogWarning("The native SDK is expected to be initialized by the app but it is not. " +
                               "Make sure the app initializes Sentry before the game starts. Skipping native support.");
            return;
        }
        else
        {
            // With external initialization, the game runs in its own process, where the app has not initialized the SDK.
            Logger?.LogInfo("Initializing the Android SDK");

            SentryJava.Init(options);

            Logger?.LogDebug("Validating Android SDK initialization");

            if (SentryJava.IsEnabled() is not true)
            {
                Logger?.LogError("Failed to initialize Android Native Support");
                return;
            }
        }

        Logger?.LogDebug("Configuring scope sync");

        options.NativeContextWriter = new NativeContextWriter(SentryJava);
        options.ScopeObserver = isExternal
            ? new ExternalAndroidJavaScopeObserver(options, SentryJava, isAttached)
            : new AndroidJavaScopeObserver(options, SentryJava);
        options.EnableScopeSync = true;
        options.NativeDebugImageProvider = new Native.NativeDebugImageProvider();

        if (isExternal)
        {
            UseAppReleaseAndEnvironment(options, SentryJava);

            if (isAttached)
            {
                NativePropagationContext.Use(options, SentryJava.GetTraceHeader, SentryJava.GetBaggageHeader);
            }
            else
            {
                UseMainProcessTrace(options, SentryJava);
            }

            if (options.AutoSessionTracking)
            {
                Logger?.LogDebug("Disabling session tracking - the app tracks sessions.");
                options.AutoSessionTracking = false;
            }
        }

        // When attached, the app's native SDK decides on app hang tracking.
        if (!isAttached && options.NativeAppHangTrackingEnabled && options.NdkIntegrationEnabled)
        {
            Logger?.LogDebug("Starting the app-hang heartbeat coroutine.");
            SentryMonoBehaviour.Instance.StartAppHangHeartbeat(
                SentryNative.AppHangHeartbeat,
                SentryNative.AppHangPause);
            Logger?.LogDebug("Disabling the C# ANR watchdog - sentry-native handles app hang detection.");
            options.RemoveIntegration<AnrIntegration>();
        }

        options.CrashedLastRun = () =>
        {
            Logger?.LogDebug("Checking for 'CrashedLastRun'");

            var crashedLastRun = SentryJava.CrashedLastRun();
            if (crashedLastRun is null)
            {
                // Could happen if the Android SDK wasn't initialized before the .NET layer.
                Logger?
                    .LogWarning(
                        "Unclear from the native SDK if the previous run was a crash. Assuming it was not.");
                crashedLastRun = false;
            }
            else
            {
                Logger?.LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);
            }

            return crashedLastRun.Value;
        };

        try
        {
            Logger?.LogDebug("Reinstalling native backend.");

            // At this point Unity has taken the signal handler and will not invoke the original handler (Sentry)
            // So we register our backend once more to make sure user-defined data is available in the crash report.
            SentryNative.ReinstallBackend();
        }
        catch (Exception e)
        {
            Logger?.LogError(
                e, "Failed to reinstall backend. Captured native crashes will miss scope data and tag.");
        }

        // When attached, the app owns the SDK's lifetime.
        if (!isAttached)
        {
            options.NativeSupportCloseCallback += () => Close(options);
        }

        Logger?.LogDebug("Fetching installation ID");

        var installationId = SentryJava.GetInstallationId();
        if (!string.IsNullOrEmpty(installationId))
        {
            options.DefaultUserId = installationId;
        }
        else
        {
            Logger?.LogDebug("Failed to fetch 'Installation ID' from the native SDK.");
        }

        Logger?.LogInfo("Successfully configured the Android SDK");
    }

    // The game runs in its own process and continues the trace the app was in when it showed the game.
    private static void UseMainProcessTrace(SentryUnityOptions options, ISentryJava sentryJava)
    {
        string? traceHeader = null;

        void Refresh()
        {
            // The app's SDK rewrites the file, so a read can come up empty. The previous trace still applies.
            if (sentryJava.GetMainProcessTraceHeader() is { } refreshed && SentryTraceHeader.Parse(refreshed) is { } header)
            {
                Logger?.LogDebug("Continuing the app's trace: {0}", refreshed);
                traceHeader = refreshed;
                sentryJava.SetTrace(header.TraceId, header.SpanId);
            }
        }

        Refresh();
        var monoBehaviour = SentryMonoBehaviour.Instance;
        monoBehaviour.ApplicationResuming += Refresh;
        options.NativeSupportCloseCallback += () => monoBehaviour.ApplicationResuming -= Refresh;
        NativePropagationContext.Use(options, () => traceHeader, () => null);
    }

    // Events from the game belong to the app's release and environment, unless they were set in the options.
    // In its own process, the game's SDK took them from the app's SDK when it initialized.
    private static void UseAppReleaseAndEnvironment(SentryUnityOptions options, ISentryJava sentryJava)
    {
        if (options.Release == options.DefaultRelease && sentryJava.GetOption("getRelease") is { } release)
        {
            Logger?.LogDebug("Using the app's release: {0}", release);
            options.Release = release;
            options.Distribution ??= sentryJava.GetOption("getDist");
        }

        if (options.Environment == options.DefaultEnvironment && sentryJava.GetOption("getEnvironment") is { } environment)
        {
            Logger?.LogDebug("Using the app's environment: {0}", environment);
            options.Environment = environment;
        }
    }

    /// <summary>
    /// Closes the native Android support.
    /// </summary>
    public static void Close(SentryUnityOptions options)
    {
        Logger?.LogInfo("Attempting to close the Android SDK");

        if (!options.IsNativeSupportEnabled())
        {
            Logger?.LogDebug("Android Native Support is not enabled. Skipping closing the Android SDK");
            return;
        }

        if (SentryJava?.IsSentryJavaPresent() is not true)
        {
            Logger?.LogDebug("Failed to find Sentry Java. Skipping closing the Android SDK");
            return;
        }

        Logger?.LogDebug("Closing the Android SDK");
        SentryJava.Close();
    }
}
