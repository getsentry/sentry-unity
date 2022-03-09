using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// Access to the Sentry native support on Android.
    /// </summary>
    public static class SentryNativeAndroid
    {
        /// <summary>
        /// Configures the native Android support.
        /// </summary>
        /// <param name="options">The Sentry Unity options to use.</param>
        public static void Configure(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo)
        {
            if (options.AndroidNativeSupportEnabled)
            {
                options.ScopeObserver = new AndroidJavaScopeObserver(options);
                options.EnableScopeSync = true;
                options.CrashedLastRun = () =>
                {
                    var crashedLastRun = SentryJava.CrashedLastRun();
                    if (crashedLastRun is null)
                    {
                        // Could happen if the Android SDK wasn't initialized before the .NET layer.
                        options.DiagnosticLogger?
                            .LogWarning("Unclear from the native SDK if the previous run was a crash. Assuming it was not.");
                        crashedLastRun = false;
                    }
                    else
                    {
                        options.DiagnosticLogger?
                            .LogDebug("Native Android SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);
                    }

                    return crashedLastRun.Value;
                };

                // When running on Mono, we shouldn't take over the signal handler because its used to propagate exceptions into the VM.
                // If we take over, a C# null reference ends up crashing the app.
                if (sentryUnityInfo.IL2CPP)
                {
                    // At this point Unity has taken the signal handler and will not invoke the original handler (Sentry)
                    // So we register our backend once more to make sure user-defined data is available in the crash report.
                    SentryNative.ReinstallBackend();
                }
                ApplicationAdapter.Instance.Quitting += () =>
                {
                    // Sentry Native is initialized and closed by the Java SDK, no need to call into it directly
                    options.DiagnosticLogger?.LogDebug("Closing the sentry-java SDK");
                    SentryJava.Close();
                };
            }
        }
    }
}
