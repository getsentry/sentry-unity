using System.IO;
using System.Text.Json;
using Sentry.Unity.Extensions;
using Sentry.Unity.Integrations;
using UnityEngine;

using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity
{
    public enum CompressionLevelWithAuto
    {
        Auto = -1,
        Optimal = CompressionLevel.Optimal,
        Fastest = CompressionLevel.Fastest,
        NoCompression = CompressionLevel.NoCompression,
    }
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

        internal static string GetConfigPath(string? notDefaultConfigName = null)
            => $"{Application.dataPath}/Resources/{ConfigRootFolder}/{notDefaultConfigName ?? ConfigName}.json";

        /// <summary>
        /// UPM name of Sentry Unity SDK (package.json)
        /// </summary>
        public const string PackageName = "io.sentry.unity";

        public bool Enabled { get; set; } = true;
        public bool CaptureInEditor { get; set; } = true; // Lower entry barrier, likely set to false after initial setup.
        public bool DebugOnlyInEditor { get; set; } = true;
        public SentryLevel DiagnosticsLevel { get; set; } = SentryLevel.Error; // By default logs out Error or higher.

        private CompressionLevelWithAuto _requestBodyCompressionLevel = CompressionLevelWithAuto.Auto;

        public new CompressionLevelWithAuto RequestBodyCompressionLevel
        {
            get => _requestBodyCompressionLevel;
            set
            {
                _requestBodyCompressionLevel = value;
                if (value == CompressionLevelWithAuto.Auto)
                {
                    // TODO: If WebGL, then NoCompression, else .. optimize (e.g: adapt to platform)
                    // The target platform is known when building the player, so 'auto' should resolve there(here).
                    // Since some platforms don't support GZipping fallback: no compression.
                    base.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
                }
                else
                {
                    // Auto would result in -1 set if not treated before providing the options to the Sentry .NET SDK
                    // DeflateStream would throw System.ArgumentOutOfRangeException
                    base.RequestBodyCompressionLevel = (CompressionLevel)value;
                }
            }
        }

        public UnitySentryOptions()
        {
            // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
            DetectStartupTime = StartupTimeDetectionMode.Fast;

            // Uses the game `version` as Release unless the user defined one via the Options
            Release ??= Application.version; // TODO: Should we move it out and use via IApplication something?

            Environment = Environment is { } environment
                ? environment
                : Application.isEditor // TODO: Should we move it out and use via IApplication something?
                    ? "editor"
                    : "production";

            this.AddInAppExclude("UnityEngine");
            this.AddInAppExclude("UnityEditor");
            this.AddEventProcessor(new UnityEventProcessor());
            this.AddExceptionProcessor(new UnityEventExceptionProcessor());
            this.AddIntegration(new UnityApplicationLoggingIntegration());
            this.AddIntegration(new UnityBeforeSceneLoadIntegration());
        }

        // Can't rely on Unity's OnEnable() hook.
        public UnitySentryOptions TryAttachLogger()
        {
            DiagnosticLogger = Debug
                               && (!DebugOnlyInEditor || Application.isEditor) // TODO: Should we move it out and use via IApplication something?
                ? new UnityLogger(DiagnosticsLevel)
                : null;

            return this;
        }

        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

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

            writer.WriteNumber("requestBodyCompressionLevel", (int)RequestBodyCompressionLevel);

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
                Enabled = json.GetPropertyOrNull("enabled")?.GetBoolean() ?? true,
                Dsn = json.GetPropertyOrNull("dsn")?.GetString(),
                CaptureInEditor = json.GetPropertyOrNull("captureInEditor")?.GetBoolean() ?? false,
                Debug = json.GetPropertyOrNull("debug")?.GetBoolean() ?? true,
                DebugOnlyInEditor = json.GetPropertyOrNull("debugOnlyInEditor")?.GetBoolean() ?? true,
                DiagnosticsLevel = json.GetEnumOrNull<SentryLevel>("diagnosticsLevel") ?? SentryLevel.Error,
                RequestBodyCompressionLevel = json.GetEnumOrNull<CompressionLevelWithAuto>("requestBodyCompressionLevel") ?? CompressionLevelWithAuto.Auto,
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
