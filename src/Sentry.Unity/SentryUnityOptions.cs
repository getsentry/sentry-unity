using System;
using System.IO;
using System.Text.Json;
using Sentry.Unity.Extensions;
using Sentry.Unity.Integrations;
using UnityEngine;

using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity
{
    /// <summary>
    /// Sentry Unity Options.
    /// </summary>
    /// <remarks>
    /// Options to configure Unity while extending the Sentry .NET SDK functionality.
    /// </remarks>
    public sealed class SentryUnityOptions : SentryOptions
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

        /// <summary>
        /// Whether the SDK should automatically enable or not.
        /// </summary>
        /// <remarks>
        /// At a minimum, the <see cref="Dsn"/> need to be provided.
        /// </remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Whether Sentry events should be captured while in the Unity Editor.
        /// </summary>
        // Lower entry barrier, likely set to false after initial setup.
        public bool CaptureInEditor { get; set; } = true;

        /// <summary>
        /// Whether the SDK should be in <see cref="Debug"/> mode only while in the Unity Editor.
        /// </summary>
        public bool DebugOnlyInEditor { get; set; } = true;

        private CompressionLevelWithAuto _requestBodyCompressionLevel = CompressionLevelWithAuto.Auto;

        /// <summary>
        /// The level which to compress the request body sent to Sentry.
        /// </summary>
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

        public SentryUnityOptions()
        {
            // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
            DetectStartupTime = StartupTimeDetectionMode.Fast;

            this.AddInAppExclude("UnityEngine");
            this.AddInAppExclude("UnityEditor");
            this.AddEventProcessor(new UnityEventProcessor());
            this.AddExceptionProcessor(new UnityEventExceptionProcessor());
            this.AddIntegration(new UnityApplicationLoggingIntegration());
            this.AddIntegration(new UnityBeforeSceneLoadIntegration());
            this.AddIntegration(new SceneManagerIntegration());
            this.AddIntegration(new SessionIntegration());
        }

        // Can't rely on Unity's OnEnable() hook.
        public SentryUnityOptions TryAttachLogger()
        {
            if (DiagnosticLogger is null
                && Debug
                // TODO: Move it out and use via IApplication
                && (!DebugOnlyInEditor || Application.isEditor))
            {
                DiagnosticLogger = new UnityLogger(DiagnosticLevel);
            }

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
            writer.WriteNumber("diagnosticLevel", (int)DiagnosticLevel);
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

        public static SentryUnityOptions FromJson(JsonElement json)
            => new()
            {
                Enabled = json.GetPropertyOrNull("enabled")?.GetBoolean() ?? true,
                Dsn = json.GetPropertyOrNull("dsn")?.GetString(),
                CaptureInEditor = json.GetPropertyOrNull("captureInEditor")?.GetBoolean() ?? false,
                Debug = json.GetPropertyOrNull("debug")?.GetBoolean() ?? true,
                DebugOnlyInEditor = json.GetPropertyOrNull("debugOnlyInEditor")?.GetBoolean() ?? true,
                DiagnosticLevel = json.GetEnumOrNull<SentryLevel>("diagnosticLevel") ?? SentryLevel.Error,
                RequestBodyCompressionLevel = json.GetEnumOrNull<CompressionLevelWithAuto>("requestBodyCompressionLevel") ?? CompressionLevelWithAuto.Auto,
                AttachStacktrace = json.GetPropertyOrNull("attachStacktrace")?.GetBoolean() ?? false,
                SampleRate = json.GetPropertyOrNull("sampleRate")?.GetSingle() ?? 1.0f,
                Release = json.GetPropertyOrNull("release")?.GetString(),
                Environment = json.GetPropertyOrNull("environment")?.GetString()
            };

        /// <summary>
        /// Try load SentryOptions.json in a platform-agnostic way.
        /// </summary>
        public static SentryUnityOptions? LoadFromUnity()
        {
            // We should use `TextAsset` for read-only access in runtime. It's platform agnostic.
            var sentryOptionsTextAsset = Resources.Load<TextAsset>($"{ConfigRootFolder}/{ConfigName}");
            if (sentryOptionsTextAsset == null)
            {
                // Config not found.
                return null;
            }
            using var jsonDocument = JsonDocument.Parse(sentryOptionsTextAsset.bytes);
            return FromJson(jsonDocument.RootElement).TryAttachLogger();
        }

        public void SaveToUnity(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(path, FileMode.Create);
            using var writer = new Utf8JsonWriter(fileStream);
            WriteTo(writer);
        }
    }

    /// <summary>
    /// <see cref="CompressionLevel"/> with an additional value for Automatic
    /// </summary>
    public enum CompressionLevelWithAuto
    {
        /// <summary>
        /// The Unity SDK will attempt to choose the best option for the target player.
        /// </summary>
        Auto = -1,
        /// <summary>
        /// The compression operation should be optimally compressed, even if the operation takes a longer time (and CPU) to complete.
        /// Not supported on IL2CPP.
        /// </summary>
        Optimal = CompressionLevel.Optimal,
        /// <summary>
        /// The compression operation should complete as quickly as possible, even if the resulting data is not optimally compressed.
        /// Not supported on IL2CPP.
        /// </summary>
        Fastest = CompressionLevel.Fastest,
        /// <summary>
        /// No compression should be performed.
        /// </summary>
        NoCompression = CompressionLevel.NoCompression,
    }
}
