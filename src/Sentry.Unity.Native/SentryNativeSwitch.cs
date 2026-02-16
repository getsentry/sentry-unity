#if SENTRY_NATIVE_SWITCH
using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Native;

/// <summary>
/// Configure Sentry for Nintendo Switch
/// </summary>
public static class SentryNativeSwitch
{
    // P/Invoke to sentry_switch_utils.cpp
    [DllImport("__Internal")]
    private static extern int sentry_switch_utils_mount();

    [DllImport("__Internal")]
    private static extern IntPtr sentry_switch_utils_get_cache_path();

    [DllImport("__Internal")]
    private static extern int sentry_switch_utils_is_mounted();

    [DllImport("__Internal")]
    private static extern void sentry_switch_utils_unmount();

    [DllImport("__Internal")]
    private static extern IntPtr sentry_switch_utils_get_default_user_id();

    private static IDiagnosticLogger? Logger;

    /// <summary>
    /// Configures the native support for Nintendo Switch.
    /// </summary>
    /// <param name="options">The Sentry Unity options to use.</param>
    public static void Configure(SentryUnityOptions options) =>
        Configure(options, ApplicationAdapter.Instance.Platform);

    // For testing
    internal static void Configure(SentryUnityOptions options, RuntimePlatform platform)
    {
        Logger = options.DiagnosticLogger;

        Logger?.LogInfo("Attempting to configure native support via the Native SDK");

        // Switch has limited file write access - disable for now
        options.DisableFileWrite = true;

        if (options.Il2CppLineNumberSupportEnabled)
        {
            options.Il2CppLineNumberSupportEnabled = false;
            Logger?.LogWarning("IL2CPP line number support is not available on Nintendo Switch - disabling.");
        }

        if (options.AutoSessionTracking)
        {
            Logger?.LogDebug("Disabling automatic session tracking on Switch due to limited file access.");
            options.AutoSessionTracking = false;
        }

        if (options.BackgroundWorker is null)
        {
            Logger?.LogDebug("Setting WebBackgroundWorker as background.");
            options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
        }

        // Bailing late so the options can respect the platform limitations
        if (!options.IsNativeSupportEnabled(platform))
        {
            Logger?.LogDebug("Native support is disabled for '{0}'.", platform);
            return;
        }

        Logger?.LogDebug("Mounting temporary storage for sentry-switch.");

        if (sentry_switch_utils_mount() != 1)
        {
            Logger?.LogError(
                "Failed to mount temporary storage - Native scope sync will be disabled. " +
                "Ensure 'TemporaryStorageSize' is set in the '.nmeta file'.");
            return;
        }

        var cachePath = Marshal.PtrToStringAnsi(sentry_switch_utils_get_cache_path());
        if (string.IsNullOrEmpty(cachePath))
        {
            Logger?.LogError("Failed to get cache path from mounted storage - Native scope sync will be disabled.");
            return;
        }

        // Setting CacheDirectoryPath here so that GetDatabasePath() finds it in step 1 of its
        // cascading resolution. On Switch, DisableFileWrite is true, which prevents the .NET
        // CachingTransport from using this path for offline caching — it only serves as the
        // base for the native database path.
        Logger?.LogDebug("Setting native cache directory: {0}", cachePath);
        options.CacheDirectoryPath = cachePath;

        try
        {
            Logger?.LogDebug("Initializing the native SDK.");
            if (!SentryNativeBridge.Init(options))
            {
                Logger?.LogError("Failed to initialize sentry-switch - Native scope sync will be disabled.");
                sentry_switch_utils_unmount();
                return;
            }
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Sentry native initialization failed - Native scope sync will be disabled.");
            sentry_switch_utils_unmount();
            return;
        }

        ApplicationAdapter.Instance.Quitting += () =>
        {
            Logger?.LogDebug("Closing the sentry-switch SDK.");
            SentryNativeBridge.Close();
            sentry_switch_utils_unmount();
        };

        Logger?.LogDebug("Setting up native scope sync.");
        options.ScopeObserver = new NativeScopeObserver(options);
        options.EnableScopeSync = true;
        options.NativeContextWriter = new NativeContextWriter();
        options.NativeDebugImageProvider = new NativeDebugImageProvider();

        var defaultUserIdPtr = sentry_switch_utils_get_default_user_id();
        var defaultUserId = Marshal.PtrToStringAnsi(defaultUserIdPtr);
        if (!string.IsNullOrEmpty(defaultUserId))
        {
            Logger?.LogDebug("Using Default User ID: {0}", defaultUserId);
            options.DefaultUserId = defaultUserId;
        }
    }
}
#endif
