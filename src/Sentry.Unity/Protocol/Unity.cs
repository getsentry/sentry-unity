using System.IO;
using System.Text;
using System.Text.Json;
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

        internal Unity Clone()
            => new()
            {
                InstallMode = InstallMode,
                CopyTextureSupport = CopyTextureSupport,
                RenderingThreadingMode = RenderingThreadingMode
            };

        public void WriteTo(Utf8JsonWriter writer)
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

            writer.WriteEndObject();
        }

        public static Unity FromJson(JsonElement json)
            => new ()
            {
                InstallMode = json.GetPropertyOrNull("install_mode")?.GetString(),
                CopyTextureSupport = json.GetPropertyOrNull("copy_texture_support")?.GetString(),
                RenderingThreadingMode = json.GetPropertyOrNull("rendering_threading_mode")?.GetString()
            };

        /*
         * TODO:
         * Logic from 'Sentry.Tests.Helpers.JsonSerializableExtensions'.
         * Need to reuse when 'Sentry.IJsonSerializable' is public (internal for now).
        */
        public string ToJsonString()
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            WriteTo(writer);
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
