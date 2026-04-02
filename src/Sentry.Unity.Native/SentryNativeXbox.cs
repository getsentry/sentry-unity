#if SENTRY_NATIVE_XBOX
using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;

namespace Sentry.Unity.Native;

/// <summary>
/// Xbox-specific helpers for Sentry native support.
/// </summary>
/// <remarks>
/// On Xbox, <c>Application.persistentDataPath</c> returns an empty string for packaged (installed) builds.
/// The writable storage must be resolved via the Xbox Persistent Local Storage (PLS) API, which requires
/// <c>PersistentLocalStorage</c> to be configured in the game's <c>MicrosoftGame.config</c>.
/// </remarks>
internal static class SentryNativeXbox
{
    [DllImport("sentry")]
    private static extern IntPtr sentry_xbox_utils_get_pls_path();

    /// <summary>
    /// Resolves the Xbox Persistent Local Storage path and sets <see cref="SentryOptions.CacheDirectoryPath"/>.
    /// </summary>
    /// <remarks>
    /// Called from <see cref="SentryNative.Configure"/> before native SDK initialization.
    /// If PLS is not available (e.g. not configured in MicrosoftGame.config), the cache directory
    /// is left unset. The SDK will operate without offline caching, session persistence, or native crash reporting.
    /// </remarks>
    internal static void ResolveStoragePath(SentryUnityOptions options, IDiagnosticLogger? logger)
    {
        if (!string.IsNullOrEmpty(options.CacheDirectoryPath))
        {
            logger?.LogWarning("The 'CacheDirectoryPath' has already been set by the user. " +
                "Storage path resolution will be skipped.");
            return;
        }

        string? plsPath = null;
        try
        {
            var plsPathPtr = sentry_xbox_utils_get_pls_path();
            plsPath = Marshal.PtrToStringAnsi(plsPathPtr);
        }
        catch (EntryPointNotFoundException)
        {
            logger?.LogWarning("Failed to find 'sentry_xbox_utils_get_pls_path' in sentry.dll.");
        }

        if (!string.IsNullOrEmpty(plsPath))
        {
            logger?.LogDebug("Setting Persistent Local Storage as cache directory path: '{0}'", plsPath);
            options.CacheDirectoryPath = plsPath;
        }
        else
        {
            logger?.LogWarning("Failed to retrieve Xbox Persistent Local Storage path. " +
                "Ensure 'PersistentLocalStorage' is configured in MicrosoftGame.config. " +
                "Offline caching, session persistence, and native crash support will be disabled.");
        }
    }
}
#endif
