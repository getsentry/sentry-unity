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

        [field: SerializeField] public string Dsn { get; set; } = "";

        [field: SerializeField] public float SampleRate { get; set; } = 1.0f;

        [field: SerializeField] public bool AttachStacktrace { get; set; }
        [field: SerializeField] public string ReleaseOverride { get; set; } = "";
        [field: SerializeField] public string EnvironmentOverride { get; set; } = "";
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
                var options = LoadFromJson(sentryOptionsTextAsset);
                return options;
            }

            var scriptableOptions = Resources.Load<ScriptableSentryUnityOptions>($"{ConfigRootFolder}/{ConfigName}");
            if (scriptableOptions != null)
            {
                return LoadFromSerializableObject(scriptableOptions);
            }

            return null;
        }

        private static SentryUnityOptions? LoadFromJson(TextAsset sentryOptionsTextAsset)
        {
            var options = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(options);

            var json = JsonDocument.Parse(sentryOptionsTextAsset.bytes).RootElement;

            if (json.GetPropertyOrNull("enabled") is {} enabled)
            {
                options.Enabled = enabled.GetBoolean();
            }
            if (json.GetPropertyOrNull("dsn") is {} dsn)
            {
                options.Dsn = dsn.GetString();
            }
            if (json.GetPropertyOrNull("captureInEditor") is {} captureInEditor)
            {
                options.CaptureInEditor = captureInEditor.GetBoolean();
            }
            if (json.GetPropertyOrNull("debug") is {} debug)
            {
                options.Debug = debug.GetBoolean();
            }
            if (json.GetPropertyOrNull("debugOnlyInEditor") is {} debugOnlyInEditor)
            {
                options.DebugOnlyInEditor = debugOnlyInEditor.GetBoolean();
            }
            if (json.GetEnumOrNull<SentryLevel>("diagnosticLevel") is {} diagnosticLevel)
            {
                options.DiagnosticLevel = diagnosticLevel;
            }
            if (json.GetEnumOrNull<CompressionLevelWithAuto>("requestBodyCompressionLevel") is {} requestBodyCompressionLevel)
            {
                options.RequestBodyCompressionLevel = requestBodyCompressionLevel;
            }
            if (json.GetPropertyOrNull("attachStacktrace") is {} attachStacktrace)
            {
                options.AttachStacktrace = attachStacktrace.GetBoolean();
            }
            if (json.GetPropertyOrNull("sampleRate") is {} sampleRate)
            {
                options.SampleRate = sampleRate.GetSingle();
            }
            if (json.GetPropertyOrNull("release") is {} release)
            {
                options.Release = release.GetString();
            }
            if (json.GetPropertyOrNull("environment") is {} environment)
            {
                options.Environment = environment.GetString();
            }

            return options;
        }

        private static SentryUnityOptions? LoadFromSerializableObject(ScriptableSentryUnityOptions scriptableOptions)
        {
            var options = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(options);

            options.Enabled = scriptableOptions.Enabled;
            options.CaptureInEditor = scriptableOptions.CaptureInEditor;
            options.Dsn = scriptableOptions.Dsn;
            options.SampleRate = scriptableOptions.SampleRate;
            options.AttachStacktrace = scriptableOptions.AttachStacktrace;

            if (!string.IsNullOrEmpty(scriptableOptions.ReleaseOverride))
            {
                options.Release = scriptableOptions.ReleaseOverride;
            }

            if (!string.IsNullOrEmpty(scriptableOptions.EnvironmentOverride))
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
