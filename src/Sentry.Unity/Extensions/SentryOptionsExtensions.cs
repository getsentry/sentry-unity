using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity.Extensions
{
    internal static class SentryOptionsExtensions
    {
        public static void ConfigureRelease(this SentryOptions sentryOptions)
            // Uses the game `version` as Release unless the user defined one via the Options
            => sentryOptions.Release = sentryOptions.Release is { } release
                ? release
                : Application.version;

        public static void ConfigureEnvironment(this SentryOptions sentryOptions)
            => sentryOptions.Environment = sentryOptions.Environment is { } environment
                ? environment
                : Application.isEditor
                    ? "editor"
#if DEVELOPMENT_BUILD
                    : "development";
#else
                    : "production";
#endif

        public static void ConfigureRequestBodyCompressionLevel(this UnitySentryOptions unitySentryOptions)
        {
            // The target platform is known when building the player, so 'auto' should resolve there.
            // Since some platforms don't support GZipping fallback no no compression.
            if (unitySentryOptions.DisableAutoCompression)
            {
                return;
            }

            unitySentryOptions.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
        }
    }
}
