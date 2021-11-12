using System;
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

        [field: SerializeField] public bool UploadSymbols { get; set; } = true;
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

        internal static SentryCliOptions CreateCliOptions(string? configName)
        {
            var cliOptions = CreateInstance<SentryCliOptions>();

            AssetDatabase.CreateAsset(cliOptions, GetConfigPath(configName));
            AssetDatabase.SaveAssets();

            return cliOptions;
        }
    }
}
