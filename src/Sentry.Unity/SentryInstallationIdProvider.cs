using System;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity;

internal static class SentryInstallationIdProvider
{
    public static string? GetInstallationId(SentryUnityOptions options)
    {
        if (!IsPlatformWithRestrictedFileAccess())
        {
            var fileId = TryGetPersistentFileId(options);
            if (!string.IsNullOrEmpty(fileId))
            {
                return fileId;
            }
        }

        var prefsId = TryGetPlayerPrefsId(options.DiagnosticLogger);
        if (!string.IsNullOrEmpty(prefsId))
        {
            return prefsId;
        }

        options.DiagnosticLogger?.LogDebug("Falling back to session-only installation ID (will not persist across restarts).");
        return Guid.NewGuid().ToString();
    }

    private static string? TryGetPersistentFileId(SentryUnityOptions options)
    {
        if (options.DisableFileWrite)
        {
            options.DiagnosticLogger?.LogDebug("File write has been disabled via options. Skipping persisting installation ID.");
            return null;
        }

        var directoryPath = options.TryGetDsnSpecificCacheDirectoryPath();
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            options.DiagnosticLogger?.LogDebug("Cache directory path is not available. Skipping persisting installation ID.");
            return null;
        }

        try
        {
            var fileSystem = options.FileSystem;
            if (!fileSystem.CreateDirectory(directoryPath!))
            {
                options.DiagnosticLogger?.LogDebug("Failed to create Sentry cache directory for installation ID file ({0}).", directoryPath);
                return null;
            }

            var filePath = Path.Combine(directoryPath, ".installation");
            if (fileSystem.FileExists(filePath))
            {
                var existingId = fileSystem.ReadAllTextFromFile(filePath)?.Trim();
                if (!string.IsNullOrEmpty(existingId))
                {
                    options.DiagnosticLogger?.LogDebug("Using existing persistent installation ID.");
                    return existingId;
                }
            }

            var newId = Guid.NewGuid().ToString();
            if (!fileSystem.WriteAllTextToFile(filePath, newId))
            {
                options.DiagnosticLogger?.LogDebug("Failed to write installation ID to file ({0}).", filePath);
                return null;
            }

            options.DiagnosticLogger?.LogDebug("Generated and saved new persistent installation ID to '{0}'.", filePath);
            return newId;
        }
        catch (Exception e)
        {
            options.DiagnosticLogger?.LogError(e, "Persistent file storage failed, trying PlayerPrefs fallback.");
            return null;
        }
    }

    private static string? TryGetPlayerPrefsId(IDiagnosticLogger? logger)
    {
        try
        {
            var prefsKey = $"sentry_installation_id_{Application.identifier}";
            if (PlayerPrefs.HasKey(prefsKey))
            {
                var existingId = PlayerPrefs.GetString(prefsKey);
                if (!string.IsNullOrEmpty(existingId))
                {
                    logger?.LogDebug("Using existing PlayerPrefs installation ID.");
                    return existingId;
                }
            }

            var newId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(prefsKey, newId);
            PlayerPrefs.Save();
            logger?.LogDebug("Generated and saved new PlayerPrefs installation ID.");
            return newId;
        }
        catch (Exception e)
        {
            logger?.LogError(e, "PlayerPrefs storage failed, using session-only fallback.");
            return null;
        }
    }

    private static bool IsPlatformWithRestrictedFileAccess(IApplication? application = null)
    {
        application ??= ApplicationAdapter.Instance;
        switch (application.Platform)
        {
            case RuntimePlatform.Switch:
            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
            case RuntimePlatform.GameCoreXboxOne:
            case RuntimePlatform.WebGLPlayer:
                return true;

            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
            default:
                return false;
        }
    }
}
