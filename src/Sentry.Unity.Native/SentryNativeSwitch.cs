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
    // P/Invoke to SentrySwitchHelpers.cpp
    [DllImport("__Internal")]
    private static extern int SentrySwitchHelpers_Mount();

    [DllImport("__Internal")]
    private static extern IntPtr SentrySwitchHelpers_GetCachePath();

    [DllImport("__Internal")]
    private static extern int SentrySwitchHelpers_IsMounted();

    [DllImport("__Internal")]
    private static extern void SentrySwitchHelpers_Unmount();

    [DllImport("__Internal")]
    private static extern IntPtr SentrySwitchHelpers_GetDefaultUserId();

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
            options.DiagnosticLogger?.LogWarning("IL2CPP line number support is not available on Nintendo Switch - disabling.");
        }

        if (options.AutoSessionTracking)
        {
            options.DiagnosticLogger?.LogDebug("Disabling automatic session tracking on Switch due to limited file access.");
            options.AutoSessionTracking = false;
        }

        if (options.BackgroundWorker is null)
        {
            options.DiagnosticLogger?.LogDebug("Setting WebBackgroundWorker as background.");
            options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
        }

        // Bailing late so the options can respect the platform limitations
        if (!options.IsNativeSupportEnabled(platform))
        {
            Logger?.LogDebug("Native support is disabled for '{0}'.", platform);
            return;
        }

        options.DiagnosticLogger?.LogDebug("Mounting temporary storage for sentry-switch.");

        if (SentrySwitchHelpers_Mount() != 1)
        {
            options.DiagnosticLogger?.LogError(
                "Failed to mount temporary storage - Native scope sync will be disabled. " +
                "Ensure 'TemporaryStorageSize' is set in the '.nmeta file'.");
            return;
        }

        var cachePath = Marshal.PtrToStringAnsi(SentrySwitchHelpers_GetCachePath());
        if (string.IsNullOrEmpty(cachePath))
        {
            options.DiagnosticLogger?.LogError("Failed to get cache path from mounted storage - Native scope sync will be disabled.");
            return;
        }

        options.DiagnosticLogger?.LogDebug("Setting native cache directory: {0}", cachePath);
        options.CacheDirectoryPath = cachePath;

        try
        {
            options.DiagnosticLogger?.LogDebug("Initializing the native SDK.");
            if (!SentryNativeBridge.Init(options))
            {
                options.DiagnosticLogger?.LogError("Failed to initialize sentry-switch - Native scope sync will be disabled.");
                SentrySwitchHelpers_Unmount();
                return;
            }
        }
        catch (Exception e)
        {
            options.DiagnosticLogger?.LogError(e, "Sentry native initialization failed - Native scope sync will be disabled.");
            SentrySwitchHelpers_Unmount();
            return;
        }

        ApplicationAdapter.Instance.Quitting += () =>
        {
            options.DiagnosticLogger?.LogDebug("Closing the sentry-switch SDK.");
            SentryNativeBridge.Close();
            SentrySwitchHelpers_Unmount();
        };

        options.DiagnosticLogger?.LogDebug("Setting up native scope sync.");
        options.ScopeObserver = new NativeScopeObserver(options);
        options.EnableScopeSync = true;
        options.NativeContextWriter = new NativeContextWriter();
        options.NativeDebugImageProvider = new NativeDebugImageProvider();

        var defaultUserIdPtr = SentrySwitchHelpers_GetDefaultUserId();
        var defaultUserId = Marshal.PtrToStringAnsi(defaultUserIdPtr);
        if (!string.IsNullOrEmpty(defaultUserId))
        {
            options.DiagnosticLogger?.LogDebug("Using Default User ID: {0}", defaultUserId);
            options.DefaultUserId = defaultUserId;
        }
    }
}
#endif
