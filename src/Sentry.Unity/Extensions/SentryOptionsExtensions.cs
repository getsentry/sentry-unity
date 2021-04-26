using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity.Extensions
{
    internal static class SentryOptionsExtensions
    {
        /*public static void ConfigureLogger(this SentryOptions sentryOptions, UnitySentryOptions unitySentryOptions)
        {
            if (unitySentryOptions.Logger == null)
            {
                return;
            }

            sentryOptions.Debug = unitySentryOptions.Debug;
            sentryOptions.DiagnosticLogger = unitySentryOptions.Logger;
            sentryOptions.DiagnosticLevel = unitySentryOptions.DiagnosticsLevel;
        }*/

        public static void ConfigureRelease(this SentryOptions sentryOptions)
            // Uses the game `version` as Release
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
            // TODO: Hack. 'RequestBodyCompressionLevel' properties differ in type. Should we stick to 'SentryOptions.CompressionLevel'?
            var sentryOptions = (SentryOptions)unitySentryOptions;
            sentryOptions.RequestBodyCompressionLevel = unitySentryOptions.RequestBodyCompressionLevel switch
            {
                SentryUnityCompression.Fastest => CompressionLevel.Fastest,
                SentryUnityCompression.Optimal => CompressionLevel.Optimal,
                // The target platform is known when building the player, so 'auto' should resolve there.
                // Since some platforms don't support GZipping fallback no no compression.
                SentryUnityCompression.Auto or SentryUnityCompression.NoCompression or _ => CompressionLevel.NoCompression,
            };
        }
    }
}
