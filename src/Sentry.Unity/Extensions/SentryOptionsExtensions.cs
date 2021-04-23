using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity.Extensions
{
    internal static class SentryOptionsExtensions
    {
        public static void ConfigureLogger(this SentryOptions sentryOptions, UnitySentryOptions unitySentryOptions)
        {
            if (unitySentryOptions.Logger == null)
            {
                return;
            }

            sentryOptions.Debug = unitySentryOptions.Debug;
            sentryOptions.DiagnosticLogger = unitySentryOptions.Logger;
            sentryOptions.DiagnosticLevel = unitySentryOptions.DiagnosticsLevel;
        }

        public static void ConfigureRelease(this SentryOptions sentryOptions, UnitySentryOptions unitySentryOptions)
            // Uses the game `version` as Release
            => sentryOptions.Release = unitySentryOptions.Release is { } release
                ? release
                : Application.version;

        public static void ConfigureEnvironment(this SentryOptions sentryOptions, UnitySentryOptions unitySentryOptions)
            => sentryOptions.Environment = unitySentryOptions.Environment is { } environment
                ? environment
                : Application.isEditor
                    ? "editor"
                    // TODO: should we remove #if?
#if DEVELOPMENT_BUILD
                    : "development";
#else
                    : "production";
#endif

        public static void ConfigureRequestBodyCompressionLevel(this SentryOptions sentryOptions, UnitySentryOptions unitySentryOptions)
            => sentryOptions.RequestBodyCompressionLevel = unitySentryOptions.RequestBodyCompressionLevel switch
            {
                SentryUnityCompression.Fastest => CompressionLevel.Fastest,
                SentryUnityCompression.Optimal => CompressionLevel.Optimal,
                // The target platform is known when building the player, so 'auto' should resolve there.
                // Since some platforms don't support GZipping fallback no no compression.
                SentryUnityCompression.Auto => CompressionLevel.NoCompression,
                SentryUnityCompression.NoCompression => CompressionLevel.NoCompression,
                _ => CompressionLevel.NoCompression
            };

        public static void RegisterInAppExclude(this SentryOptions sentryOptions)
        {
            // Required configurations to integrate with Unity
            sentryOptions.AddInAppExclude("UnityEngine");
            sentryOptions.AddInAppExclude("UnityEditor");
        }

        public static void RegisterEventProcessors(this SentryOptions sentryOptions)
        {
            sentryOptions.AddEventProcessor(new UnityEventProcessor());
            sentryOptions.AddExceptionProcessor(new UnityEventExceptionProcessor());
        }
    }
}
