using System;
using Sentry.Integrations;
using Sentry.Unity.Extensions;
using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    public sealed class SentryUnity
    {
        public static void Init(UnitySentryOptions unitySentryOptions)
        {
            // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
            unitySentryOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;

            unitySentryOptions.ConfigureRelease();
            unitySentryOptions.ConfigureEnvironment();
            unitySentryOptions.ConfigureRequestBodyCompressionLevel();
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
