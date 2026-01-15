using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Native;

/// <summary>
/// Configure Sentry for Nintendo Switch
/// </summary>
public static class SentryNativeSwitch
{
    /// <summary>
    /// Configures the native support for Nintendo Switch.
    /// </summary>
    /// <param name="options">The Sentry Unity options to use.</param>
    public static void Configure(SentryUnityOptions options)
    {
        options.DiagnosticLogger?.LogDebug("Updating configuration for Nintendo Switch.");

        // Switch has limited file write access - disable to avoid crashes
        options.DisableFileWrite = true;

        // Auto session tracking requires reliable file access
        if (options.AutoSessionTracking)
        {
            options.DiagnosticLogger?.LogDebug("Disabling automatic session tracking on Switch (limited file access).");
            options.AutoSessionTracking = false;
        }

        // Use WebBackgroundWorker for more reliable background execution on Switch
        if (options.BackgroundWorker is null)
        {
            options.DiagnosticLogger?.LogDebug("Using WebBackgroundWorker for background execution on Switch.");
            options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
        }

        // Log the cache directory paths for debugging
        options.DiagnosticLogger?.LogDebug("CacheDirectoryPath (from Unity): {0}", options.CacheDirectoryPath ?? "(null)");
        var cacheDir = SentryNativeBridge.GetCacheDirectory(options);
        options.DiagnosticLogger?.LogDebug("Switch native cache directory (for sentry-native): {0}", cacheDir);

        // Validate path format - Switch requires paths in "mountname:/path" format
        if (!cacheDir.Contains(":"))
        {
            options.DiagnosticLogger?.LogWarning(
                "Switch native cache directory path '{0}' may not be in the correct format. " +
                "Nintendo Switch requires mounted storage paths like 'mountname:/.sentry-native'. " +
                "Ensure storage is mounted (e.g., nn::fs::MountTemporaryStorage) and set " +
                "CacheDirectoryPath to the mounted path.", cacheDir);
        }

        // Initialize native crash handling via sentry-native
        try
        {
            if (!SentryNativeBridge.Init(options))
            {
                options.DiagnosticLogger?.LogWarning(
                    "Sentry native initialization failed - native crashes are not captured. " +
                    "On Switch, ensure you have mounted writable storage and set CacheDirectoryPath " +
                    "to a valid path like 'temp:/' after calling nn::fs::MountTemporaryStorage(\"temp\").");
                return;
            }
        }
        catch (Exception e)
        {
            options.DiagnosticLogger?.LogError(e, "Sentry native initialization failed - native crashes are not captured.");
            return;
        }
    }
}
