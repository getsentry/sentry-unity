using System;
using System.IO;
using System.Text.Json;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity
{
    public enum SentryUnityCompression
    {
        Auto = 0,
        Optimal = 1,
        Fastest = 2,
        NoCompression = 3
    }

    public sealed class UnitySentryOptions
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

        public bool Enabled { get; set; } = true;
        public bool CaptureInEditor { get; set; } = true; // Lower entry barrier, likely set to false after initial setup.
        public string? Dsn { get; set; }
        public bool Debug { get; set; } = true; // By default on only
        public bool DebugOnlyInEditor { get; set; } = true;
        public SentryLevel DiagnosticsLevel { get; set; } = SentryLevel.Error; // By default logs out Error or higher.
        // Ideally this would be per platform
        // Auto allows us to try figure out things in the SDK depending on the platform. Any other value means an explicit user choice.
        public SentryUnityCompression RequestBodyCompressionLevel { get; set; } = SentryUnityCompression.Auto;
        public bool AttachStacktrace { get; set; }
        public float SampleRate { get; set; } = 1.0f;

        public IDiagnosticLogger? Logger { get; private set; }
        public string? Release { get; set; }
        public string? Environment { get; set; }

        // Can't rely on Unity's OnEnable() hook.
        public UnitySentryOptions TryAttachLogger()
        {
            Logger = Debug
                     && (!DebugOnlyInEditor || Application.isEditor)
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
            writer.WriteNumber("requestBodyCompressionLevel", (int)RequestBodyCompressionLevel);
            writer.WriteBoolean("attachStacktrace", AttachStacktrace);
            writer.WriteNumber("sampleRate", SampleRate);

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
                RequestBodyCompressionLevel = json.GetEnumOrNull<SentryUnityCompression>("requestBodyCompressionLevel") ?? SentryUnityCompression.Auto,
                AttachStacktrace = json.GetPropertyOrNull("debugOnlyInEditor")?.GetBoolean() ?? false,
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

    internal static class JsonExtensions
    {
        // From Sentry.Internal.Extensions.JsonExtensions
        public static JsonElement? GetPropertyOrNull(this JsonElement json, string name)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (json.TryGetProperty(name, out var result))
            {
                if (json.ValueKind == JsonValueKind.Undefined ||
                    json.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }

                return result;
            }

            return null;
        }

        public static TEnum? GetEnumOrNull<TEnum>(this JsonElement json, string name)
            where TEnum : struct
        {
            var enumString = json.GetPropertyOrNull(name)?.ToString();
            if (string.IsNullOrWhiteSpace(enumString))
            {
                return null;
            }

            if (!Enum.TryParse(enumString, true, out TEnum value))
            {
                return null;
            }

            return value;
        }
    }
}
