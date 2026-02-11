using System;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity;

[Serializable]
public sealed class SentryCliOptions : ScriptableObject
{
    /// <summary>
    /// Sentry CLI config name for Unity
    /// </summary>
    internal const string ConfigName = "SentryCliOptions";

    public const string EditorMenuPath = "Tools -> Sentry -> Debug Symbols";

    [field: SerializeField] public bool UploadSymbols { get; set; } = true;
    [field: SerializeField] public bool UploadDevelopmentSymbols { get; set; } = false;
    [field: SerializeField] public bool UploadSources { get; set; } = false;
    [field: SerializeField] public string? UrlOverride { get; set; }
    [field: SerializeField] public string? Auth { get; set; }
    [field: SerializeField] public string? Organization { get; set; }
    [field: SerializeField] public string? Project { get; set; }
    [field: SerializeField] public bool IgnoreCliErrors { get; set; } = false;

    [field: SerializeField] public SentryCliOptionsConfiguration? CliOptionsConfiguration { get; set; }

    internal static string GetConfigPath(string? notDefaultConfigName = null)
        => $"Assets/Plugins/Sentry/{notDefaultConfigName ?? ConfigName}.asset";

    private static void MissingFieldWarning(IDiagnosticLogger? logger, string name) =>
        logger?.LogWarning("{0} missing. Please set it under {1}", name, EditorMenuPath);

    public bool IsValid(IDiagnosticLogger? logger, bool isDevelopmentBuild)
    {
        if (!UploadSymbols)
        {
            logger?.LogDebug("Automated symbols upload has been disabled.");
            return false;
        }

        if (isDevelopmentBuild && !UploadDevelopmentSymbols)
        {
            logger?.LogDebug("Automated symbols upload for development builds has been disabled.");
            return false;
        }

        var validated = true;
        if (string.IsNullOrWhiteSpace(Auth))
        {
            MissingFieldWarning(logger, "Auth Token");
            validated = false;
        }

        if (string.IsNullOrWhiteSpace(Project))
        {
            MissingFieldWarning(logger, "Project");
            validated = false;
        }

        if (!validated)
        {
            logger?.LogWarning("sentry-cli validation failed. Symbols will not be uploaded." +
                               "\nYou can disable this warning by disabling the automated symbols upload under " +
                               EditorMenuPath);
        }

        return validated;
    }
}
