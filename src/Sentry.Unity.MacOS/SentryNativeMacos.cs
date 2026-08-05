using Sentry.Extensibility;
using Sentry.Unity.iOS;
using Sentry.Unity.Native;
using Sentry.Unity.NativeUtils;

namespace Sentry.Unity.MacOS;

public static class SentryNativeMacos
{
    public static void Configure(SentryUnityOptions options)
    {
        if (options.Experimental.MacosBackend == MacosBackend.Native)
        {
            SentryNative.Configure(options);
            return;
        }

        if (!SentryPlatformServices.UnityInfo.IL2CPP)
        {
            options.DiagnosticLogger?.LogWarning(
                "Cocoa backend requires IL2CPP on macOS. Native crash reporting is disabled. " +
                "Set macOS Backend to Native or enable IL2CPP.");
            return;
        }

        SentryNativeCocoa.Configure(options);
    }

    public static void Close(SentryUnityOptions options)
    {
        if (options is null)
        {
            return;
        }

        if (options.Experimental.MacosBackend == MacosBackend.Cocoa
            && SentryPlatformServices.UnityInfo.IL2CPP)
        {
            SentryNativeCocoa.Close(options);
        }
    }
}
