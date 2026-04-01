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
    /// is left unset and the SDK will operate without offline caching or session persistence.
    /// </remarks>
    internal static void ResolveStoragePath(SentryUnityOptions options, IDiagnosticLogger? logger)
    {
        string? plsPath = null;
        try
        {
            var plsPathPtr = sentry_xbox_utils_get_pls_path();
            plsPath = Marshal.PtrToStringAnsi(plsPathPtr);
        }
        catch (EntryPointNotFoundException)
        {
            logger?.LogWarning(
                "sentry_xbox_utils_get_pls_path not found in sentry.dll. " +
                "Update the sentry-xbox native library to enable Persistent Local Storage support.");
        }

        if (!string.IsNullOrEmpty(plsPath))
        {
            logger?.LogDebug("Using Xbox Persistent Local Storage path: {0}", plsPath);
            options.CacheDirectoryPath = plsPath;
        }
        else
        {
            logger?.LogWarning(
                "Failed to retrieve Xbox Persistent Local Storage path. " +
                "Ensure 'PersistentLocalStorage' is configured in MicrosoftGame.config. " +
                "Offline caching and session persistence will be disabled.");
        }
    }
}
#endif
