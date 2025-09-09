using System;
using System.IO;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Provides game-specific installation IDs with robust fallback strategies for different Unity platforms.
/// Ensures no cross-app tracking while maintaining persistence across app restarts where possible.
/// Automatically avoids disk writes on platforms with restricted file access (consoles, WebGL, tvOS).
/// </summary>
internal static class SentryInstallationIdProvider
{
    private static string? SessionInstallationId;

    /// <summary>
    /// Gets a game-specific installation ID using a multi-tier approach:
    /// 1. Persistent file storage (most platforms)
    /// 2. PlayerPrefs (restricted platforms like Nintendo Switch)
    /// 3. Session-only GUID (final fallback)
    /// </summary>
    /// <param name="options">Sentry options for file system access and settings</param>
    /// <returns>A game-specific installation ID, or null if all methods fail</returns>
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

        // Fallback to PlayerPrefs for restricted platforms
        var prefsId = TryGetPlayerPrefsId(options.DiagnosticLogger);
        if (!string.IsNullOrEmpty(prefsId))
        {
            return prefsId;
        }

        // Final fallback: session-only GUID
        return TryGetSessionId(options.DiagnosticLogger);
    }

    private static string? TryGetPersistentFileId(SentryUnityOptions options)
    {
        // Check if file writes are disabled via options
        if (options.DisableFileWrite)
        {
            options.DiagnosticLogger?.LogDebug("File write has been disabled via options. Skipping persistent installation ID.");
            return null;
        }

        try
        {
            var directoryPath = Application.persistentDataPath;
            var fileSystem = options.FileSystem;

            // Ensure directory exists
            if (!fileSystem.CreateDirectory(directoryPath))
            {
                options.DiagnosticLogger?.LogDebug("Failed to create directory for installation ID file ({0}).", directoryPath);
                return null;
            }

            var filePath = Path.Combine(directoryPath, ".sentry-installation-id");

            // Read existing installation ID if it exists
            if (fileSystem.FileExists(filePath))
            {
                var existingId = fileSystem.ReadAllTextFromFile(filePath)?.Trim();
                if (!string.IsNullOrEmpty(existingId))
                {
                    options.DiagnosticLogger?.LogDebug("Using existing persistent installation ID.");
                    return existingId;
                }
            }

            // Generate new game-specific installation ID
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

    /// <summary>
    /// Determines if the current platform has restricted file access where disk writes should be avoided.
    /// </summary>
    /// <returns>True if the platform has restricted file access</returns>
    private static bool IsPlatformWithRestrictedFileAccess()
    {
        switch (Application.platform)
        {
            // Gaming consoles with restricted file systems
            case RuntimePlatform.Switch:
            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
            case RuntimePlatform.GameCoreXboxOne:
                return true;

            // WebGL runs in browser with limited file system access
            case RuntimePlatform.WebGLPlayer:
                return true;

            // tvOS and other restricted Apple platforms
            case RuntimePlatform.tvOS:
                return true;

            // Platforms where file operations are generally reliable
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

    private static string? TryGetPlayerPrefsId(IDiagnosticLogger? logger)
    {
        try
        {
            // Use Application.identifier to ensure game-specific key
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

    private static string? TryGetSessionId(IDiagnosticLogger? logger)
    {
        try
        {
            // Cache in static field for session persistence
            if (string.IsNullOrEmpty(SessionInstallationId))
            {
                SessionInstallationId = Guid.NewGuid().ToString();
                logger?.LogDebug("Generated session-only installation ID (will not persist across restarts).");
            }
            return SessionInstallationId;
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to generate session installation ID.");
            return null;
        }
    }
}
