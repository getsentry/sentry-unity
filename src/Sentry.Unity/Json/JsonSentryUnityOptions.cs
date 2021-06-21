using System.Text.Json;
using Sentry.Unity.Extensions;
using UnityEngine;

namespace Sentry.Unity.Json
{
    // This code exists for backward compatibility and will be removed in the future
    internal static class JsonSentryUnityOptions
    {
        /// <summary>
        /// Path for the json config for Unity
        /// </summary>
        internal static string GetConfigPath(string? notDefaultConfigName = null)
            => $"Assets/Resources/{ScriptableSentryUnityOptions.ConfigRootFolder}/{notDefaultConfigName ?? ScriptableSentryUnityOptions.ConfigName}.json";

        internal static SentryUnityOptions LoadFromJson(TextAsset sentryOptionsTextAsset)
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
                options.Dsn = string.Equals(dsn.GetString(), string.Empty) ? null : dsn.GetString();
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
                options.SampleRate = sampleRate.GetSingle() >= 1.0f ? null : sampleRate.GetSingle();
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

        internal static void ToScriptableOptions(TextAsset sentryOptionsTextAsset, ScriptableSentryUnityOptions options)
        {
            var jsonOptions = LoadFromJson(sentryOptionsTextAsset);

            options.Enabled = jsonOptions.Enabled;

            if (jsonOptions.Dsn is { } dsn)
            {
                options.Dsn = dsn;
            }

            options.CaptureInEditor = jsonOptions.CaptureInEditor;
            options.Debug = jsonOptions.Debug;
            options.DebugOnlyInEditor = jsonOptions.DebugOnlyInEditor;
            options.DiagnosticLevel = jsonOptions.DiagnosticLevel;
            options.AttachStacktrace = jsonOptions.AttachStacktrace;

            if (jsonOptions.SampleRate is { } sampleRate)
            {
                options.SampleRate = sampleRate;
            }
        }
    }
}
