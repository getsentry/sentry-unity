using System.IO;
using System.Text.Json;
using Sentry.Unity.Extensions;
using UnityEngine;

using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity
{
    // TODO: rename to `SentryUnityOptions` for consistency across dotnet Sentry SDK
    public sealed class UnitySentryOptions : SentryOptions
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
        /// UPM name of Sentry Unity SDK (package.json)
        /// </summary>
        public const string PackageName = "io.sentry.unity";

        public bool DisableProgrammaticInitialization { get; set; }

        public bool Enabled { get; set; } = true;
        public bool CaptureInEditor { get; set; } = true; // Lower entry barrier, likely set to false after initial setup.
        public bool DebugOnlyInEditor { get; set; } = true;
        public SentryLevel DiagnosticsLevel { get; set; } = SentryLevel.Error; // By default logs out Error or higher.
        public bool DisableAutoCompression { get; set; }

        // Can't rely on Unity's OnEnable() hook.
        public UnitySentryOptions TryAttachLogger()
        {
            DiagnosticLogger = Debug
                               && (!DebugOnlyInEditor || Application.isEditor)
                ? new UnityLogger(DiagnosticsLevel)
                : null;

            return this;
        }

        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteBoolean("disableProgrammaticInitialization", DisableProgrammaticInitialization);
            writer.WriteBoolean("enabled", Enabled);
            writer.WriteBoolean("captureInEditor", CaptureInEditor);

            if (!string.IsNullOrWhiteSpace(Dsn))
            {
                writer.WriteString("dsn", Dsn);
            }

            writer.WriteBoolean("debug", Debug);
            writer.WriteBoolean("debugOnlyInEditor", DebugOnlyInEditor);
            writer.WriteNumber("diagnosticsLevel", (int)DiagnosticsLevel);
            writer.WriteBoolean("attachStacktrace", AttachStacktrace);

            writer.WriteBoolean("disableAutoCompression", DisableAutoCompression);
            writer.WriteNumber("requestBodyCompressionLevel", DisableAutoCompression ? (int)RequestBodyCompressionLevel : (int)CompressionLevel.NoCompression);

            if (SampleRate != null)
            {
                writer.WriteNumber("sampleRate", SampleRate.Value);
            }

            if (!string.IsNullOrWhiteSpace(Release))
            {
                writer.WriteString("release", Release);
            }

            if (!string.IsNullOrWhiteSpace(Environment))
            {
                writer.WriteString("environment", Environment);
            }

            writer.WriteEndObject();
            writer.Flush();
        }

        public static UnitySentryOptions FromJson(JsonElement json)
            => new()
            {
                DisableProgrammaticInitialization = json.GetPropertyOrNull("disableProgrammaticInitialization")?.GetBoolean() ?? false,
                Enabled = json.GetPropertyOrNull("enabled")?.GetBoolean() ?? true,
                Dsn = json.GetPropertyOrNull("dsn")?.GetString(),
                CaptureInEditor = json.GetPropertyOrNull("captureInEditor")?.GetBoolean() ?? false,
                Debug = json.GetPropertyOrNull("debug")?.GetBoolean() ?? true,
                DebugOnlyInEditor = json.GetPropertyOrNull("debugOnlyInEditor")?.GetBoolean() ?? true,
                DiagnosticsLevel = json.GetEnumOrNull<SentryLevel>("diagnosticsLevel") ?? SentryLevel.Error,
                RequestBodyCompressionLevel = json.GetEnumOrNull<CompressionLevel>("requestBodyCompressionLevel") ?? CompressionLevel.NoCompression,
                DisableAutoCompression = json.GetPropertyOrNull("disableAutoCompression")?.GetBoolean() ?? false,
                AttachStacktrace = json.GetPropertyOrNull("attachStacktrace")?.GetBoolean() ?? false,
                SampleRate = json.GetPropertyOrNull("sampleRate")?.GetSingle() ?? 1.0f,
                Release = json.GetPropertyOrNull("release")?.GetString(),
                Environment = json.GetPropertyOrNull("environment")?.GetString()
            };

        public static UnitySentryOptions LoadFromUnity()
        {
            // We should use `TextAsset` for read-only access in runtime. It's platform agnostic.
            var sentryOptionsTextAsset = Resources.Load<TextAsset>($"{ConfigRootFolder}/{ConfigName}");
            using var jsonDocument = JsonDocument.Parse(sentryOptionsTextAsset.bytes);
            return FromJson(jsonDocument.RootElement).TryAttachLogger();
        }

        public void SaveToUnity(string path)
        {
            using var fileStream = new FileStream(path, FileMode.Create);
            using var writer = new Utf8JsonWriter(fileStream);
            WriteTo(writer);
        }
    }
}
