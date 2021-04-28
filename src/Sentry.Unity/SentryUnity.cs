using System;
using Sentry.Unity.Extensions;

namespace Sentry.Unity
{
    public sealed class SentryUnity
    {
        public static IDisposable Init(UnitySentryOptions unitySentryOptions)
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

            return SentrySdk.Init(unitySentryOptions);
        }

        public static IDisposable Init(Action<UnitySentryOptions> unitySentryOptionsConfigure)
        {
            var unitySentryOptions = new UnitySentryOptions();
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            return Init(unitySentryOptions);
        }
    }
}
