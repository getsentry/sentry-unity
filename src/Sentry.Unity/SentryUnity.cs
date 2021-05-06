using System;
using Sentry.Unity.Extensions;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    public static class SentryUnity
    {
        public static void Init(UnitySentryOptions unitySentryOptions)
        {
            // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
            unitySentryOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;

            // Uses the game `version` as Release unless the user defined one via the Options
            unitySentryOptions.Release ??= Application.version;

            // The target platform is known when building the player, so 'auto' should resolve there.
            // Since some platforms don't support GZipping fallback no no compression.
            unitySentryOptions.RequestBodyCompressionLevel = unitySentryOptions.DisableAutoCompression
                ? unitySentryOptions.RequestBodyCompressionLevel
                : System.IO.Compression.CompressionLevel.NoCompression;

            unitySentryOptions.Environment = unitySentryOptions.Environment is { } environment
                ? environment
                : Application.isEditor
                    ? "editor"
#if DEVELOPMENT_BUILD
                    : "development";
#else
                    : "production";
#endif 

            unitySentryOptions.AddInAppExclude("UnityEngine");
            unitySentryOptions.AddInAppExclude("UnityEditor");
            unitySentryOptions.AddEventProcessor(new UnityEventProcessor());
            unitySentryOptions.AddExceptionProcessor(new UnityEventExceptionProcessor());
            unitySentryOptions.AddIntegration(new UnityApplicationLoggingIntegration());
            unitySentryOptions.AddIntegration(new UnityBeforeSceneLoadIntegration());

            SentrySdk.Init(unitySentryOptions);
        }

        public static void Init(Action<UnitySentryOptions> unitySentryOptionsConfigure)
        {
            var unitySentryOptions = new UnitySentryOptions();
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            Init(unitySentryOptions);
        }
    }
}
