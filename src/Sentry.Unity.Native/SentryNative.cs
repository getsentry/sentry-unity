using Sentry.Extensibility;

namespace Sentry.Unity.Native
{
    /// <summary>
    /// Access to the Sentry native support of the sentry-native SDK.
    /// </summary>
    public static class SentryNative
    {
        /// <summary>
        /// Configures the native SDK.
        /// </summary>
        /// <param name="options">The Sentry Unity options to use.</param>
        public static void Configure(SentryUnityOptions options)
        {
            if (options.WindowsNativeSupportEnabled)
            {
                SentryNativeBridge.Init(options);
                options.ScopeObserver = new ScopeObserver("Native", options, new SentryNativeBridge());
                options.EnableScopeSync = true;
                // options.CrashedLastRun = () =>
                // {
                //     var crashedLastRun = SentryJava.CrashedLastRun();
                //     if (crashedLastRun is null)
                //     {
                //         // Could happen if the Android SDK wasn't initialized before the .NET layer.
                //         options.DiagnosticLogger?
                //             .LogWarning("Unclear from the native SDK if the previous run was a crash. Assuming it was not.");
                //         crashedLastRun = false;
                //     }
                //     else
                //     {
                //         options.DiagnosticLogger?
                //             .LogDebug("Native Android SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);
                //     }

                //     return crashedLastRun.Value;
                // };
                // At this point Unity has taken the signal handler and will not invoke the original handler (Sentry)
                // So we register our backend once more to make sure user-defined data is available in the crash report.
                SentryNativeBridge.ReinstallBackend();
            }
        }
    }
}
