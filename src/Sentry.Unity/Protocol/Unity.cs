using System.IO;
using System.Text;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Unity.Extensions;

namespace Sentry.Unity.Protocol
{
    public sealed class Unity : IJsonSerializable
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        public const string Type = "unity";

        /// <summary>
        /// Application install mode.
        /// </summary>
        /// <example>
        /// Unknown, Store, DeveloperBuild, Adhoc, Enterprise, Editor
        /// </example>
        public string? InstallMode { get; set; }

        /// <summary>
        /// Support for various copy texture cases.
        /// </summary>
        /// <example>
        /// None, Basic, Copy3D, DifferentTypes, TextureToRT, RTToTexture
        /// </example>
        public string? CopyTextureSupport { get; set; }

        /// <summary>
        /// Application's actual rendering threading mode.
        /// </summary>
        /// <example>
        /// Direct, SingleThreaded, MultiThreaded, LegacyJobified, NativeGraphicsJobs, NativeGraphicsJobsWithoutRenderThread
        /// </example>
        public string? RenderingThreadingMode { get; set; }

        /// <summary>
        /// Instructs the game to try to render at a specified frame rate.
        /// The default targetFrameRate is a special value of -1, which indicates that the game should render at the platform's default frame rate. This default rate depends on the platform.
        /// Check https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html for more info.
        /// </summary>
        public string? TargetFrameRate { get; set; }

        internal Unity Clone()
            => new()
            {
                InstallMode = InstallMode,
                CopyTextureSupport = CopyTextureSupport,
                RenderingThreadingMode = RenderingThreadingMode,
                TargetFrameRate = TargetFrameRate
            };

        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            if (!string.IsNullOrWhiteSpace(InstallMode))
            {
                writer.WriteString("install_mode", InstallMode);
            }

            if (!string.IsNullOrWhiteSpace(CopyTextureSupport))
            {
                writer.WriteString("copy_texture_support", CopyTextureSupport);
            }

            if (!string.IsNullOrWhiteSpace(RenderingThreadingMode))
            {
                writer.WriteString("rendering_threading_mode", RenderingThreadingMode);
            }

            if (!string.IsNullOrWhiteSpace(TargetFrameRate))
            {
                writer.WriteString("target_frame_rate", TargetFrameRate);
            }

            writer.WriteEndObject();
        }

        public static Unity FromJson(JsonElement json)
            => new()
            {
                InstallMode = json.GetPropertyOrNull("install_mode")?.GetString(),
                CopyTextureSupport = json.GetPropertyOrNull("copy_texture_support")?.GetString(),
                RenderingThreadingMode = json.GetPropertyOrNull("rendering_threading_mode")?.GetString(),
                TargetFrameRate = json.GetPropertyOrNull("target_frame_rate")?.GetString()
            };

        public string ToJsonString(IDiagnosticLogger? logger = null)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            WriteTo(writer, logger);
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
