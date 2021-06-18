using System.Text.Json;
using Sentry.Unity.Extensions;
using UnityEngine;

namespace Sentry.Unity
{
    public class ScriptableSentryUnityOptions : ScriptableObject
    {
        /// <summary>
        /// Relative to Assets/Resources
        /// </summary>
        public const string ConfigRootFolder = "Sentry";

        /// <summary>
        /// Main Sentry config name for Unity
        /// </summary>
        public const string ConfigName = "SentryOptions";

        /// <summary>
        /// Path for the config for Unity
        /// </summary>
        internal static string GetConfigPath(string? notDefaultConfigName = null)
            => $"Assets/Resources/{ConfigRootFolder}/{notDefaultConfigName ?? ConfigName}.asset";

        [field: SerializeField] public bool Enabled { get; set; }

        [field: SerializeField] public bool CaptureInEditor { get; set; }

        [field: SerializeField] public string Dsn { get; set; } = string.Empty;

        [field: SerializeField] public float SampleRate { get; set; } = 1.0f;

        [field: SerializeField] public bool AttachStacktrace { get; set; }
        [field: SerializeField] public string ReleaseOverride { get; set; } = string.Empty;
        [field: SerializeField] public string EnvironmentOverride { get; set; } = string.Empty;
        [field: SerializeField] public bool EnableOfflineCaching { get; set; }

        [field: SerializeField] public bool Debug { get; set; }
        [field: SerializeField] public bool DebugOnlyInEditor { get; set; }
        [field: SerializeField] public SentryLevel DiagnosticLevel { get; set; }

        public static SentryUnityOptions? LoadSentryUnityOptions()
        {
            // TODO: Deprecated and to be removed once we update far enough.
            var sentryOptionsTextAsset = Resources.Load<TextAsset>($"{ConfigRootFolder}/{ConfigName}");
            if (sentryOptionsTextAsset != null)
            {
                var options = JsonSentryUnityOptions.LoadFromJson(sentryOptionsTextAsset);
                return options;
            }

            var scriptableOptions = Resources.Load<ScriptableSentryUnityOptions>($"{ConfigRootFolder}/{ConfigName}");
            if (scriptableOptions != null)
            {
                return LoadFromScriptableObject(scriptableOptions);
            }

            return null;
        }

        internal static SentryUnityOptions LoadFromScriptableObject(ScriptableSentryUnityOptions scriptableOptions)
        {
            var options = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(options);

            options.Enabled = scriptableOptions.Enabled;
            options.CaptureInEditor = scriptableOptions.CaptureInEditor;
            options.Dsn = scriptableOptions.Dsn;
            options.SampleRate = scriptableOptions.SampleRate;
            options.AttachStacktrace = scriptableOptions.AttachStacktrace;

            if (!string.IsNullOrWhiteSpace(scriptableOptions.ReleaseOverride))
            {
                options.Release = scriptableOptions.ReleaseOverride;
            }

            if (!string.IsNullOrWhiteSpace(scriptableOptions.EnvironmentOverride))
            {
                options.Environment = scriptableOptions.EnvironmentOverride;
            }

            if (!scriptableOptions.EnableOfflineCaching)
            {
                options.CacheDirectoryPath = null;
            }

            options.Debug = scriptableOptions.Debug;
            options.DebugOnlyInEditor = scriptableOptions.DebugOnlyInEditor;
            options.DiagnosticLevel = scriptableOptions.DiagnosticLevel;

            return options;
        }
    }
}
