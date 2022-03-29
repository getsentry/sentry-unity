using System;
using Sentry.Extensibility;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
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
        [field: SerializeField] public string? Url { get; set; }
        [field: SerializeField] public string? Auth { get; set; }
        [field: SerializeField] public string? Organization { get; set; }
        [field: SerializeField] public string? Project { get; set; }

        internal static string GetConfigPath(string? notDefaultConfigName = null)
            => $"Assets/Plugins/Sentry/{notDefaultConfigName ?? ConfigName}.asset";

        internal static SentryCliOptions LoadCliOptions(string? configName = null)
        {
            var cliOptions = AssetDatabase.LoadAssetAtPath(GetConfigPath(configName),
                typeof(SentryCliOptions)) as SentryCliOptions;

            if (cliOptions is null)
            {
                cliOptions = CreateCliOptions(configName);
            }

            return cliOptions;
        }

        private static void MissingFieldWarning(IDiagnosticLogger? logger, string name) =>
            logger?.LogWarning("sentry-cli: {0} missing. Please set it under {1}", name, EditorMenuPath);

        public bool IsValid(IDiagnosticLogger? logger, bool? isDevelopmentBuild = null)
        {
            if (!UploadSymbols)
            {
                logger?.LogDebug("sentry-cli: Automated symbols upload has been disabled.");
                return false;
            }

            if ((isDevelopmentBuild ?? EditorUserBuildSettings.development) && !UploadDevelopmentSymbols)
            {
                logger?.LogDebug("sentry-cli: Automated symbols upload for development builds has been disabled.");
                return false;
            }

            var validated = true;
            if (string.IsNullOrWhiteSpace(Auth))
            {
                MissingFieldWarning(logger, "Auth name");
                validated = false;
            }

            if (string.IsNullOrWhiteSpace(Organization))
            {
                MissingFieldWarning(logger, "Organization");
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

        internal static SentryCliOptions CreateCliOptions(string? configName)
        {
            var cliOptions = CreateInstance<SentryCliOptions>();

            AssetDatabase.CreateAsset(cliOptions, GetConfigPath(configName));
            AssetDatabase.SaveAssets();

            return cliOptions;
        }
    }
}
