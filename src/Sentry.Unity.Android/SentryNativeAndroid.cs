using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using Sentry.Unity.NativeUtils;
using UnityEngine;
using UnityEngine.Analytics;

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

        // If it's not been set (in a test)
        SentryJava ??= new SentryJava(Logger);
        if (!SentryJava.IsSentryJavaPresent())
        {
            Logger?.LogError("Android Native Support has been enabled but the " +
                                               "Android SDK is missing. This could have been caused by a mismatching" +
                                               "build time / runtime configuration. Please make sure you have " +
                                               "Android Native Support enabled during build time.");
            return;
        }

        Logger?.LogDebug("Checking whether the Android SDK has already been initialized");

        if (SentryJava.IsEnabled() is true)
        {
            Logger?.LogDebug("The Android SDK is already initialized");
        }
        else
        {
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
        options.ScopeObserver = new AndroidJavaScopeObserver(options, SentryJava);
        options.EnableScopeSync = true;
        options.NativeDebugImageProvider = new Native.NativeDebugImageProvider();
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

        options.NativeSupportCloseCallback = () => Close(options);

        Logger?.LogDebug("Fetching installation ID");

        options.DefaultUserId = SentryJava.GetInstallationId();
        if (string.IsNullOrEmpty(options.DefaultUserId))
        {
            // In case we can't get an installation ID we create one and sync that down to the native layer
            Logger?.LogDebug(
                "Failed to fetch 'Installation ID' from the native SDK. Creating new 'Default User ID'.");

            // We fall back to Unity's Analytics Session Info: https://docs.unity3d.com/ScriptReference/Analytics.AnalyticsSessionInfo-userId.html
            // It's a randomly generated GUID that gets created immediately after installation helping
            // to identify the same instance of the game
            options.DefaultUserId = AnalyticsSessionInfo.userId;
            if (options.DefaultUserId is not null)
            {
                options.ScopeObserver.SetUser(new SentryUser { Id = options.DefaultUserId });
            }
            else
            {
                Logger?.LogDebug("Failed to create new 'Default User ID'.");
            }
        }

        Logger?.LogInfo("Successfully configured the Android SDK");
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
