using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using Sentry.Unity.NativeUtils;
using UnityEngine;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// Access to the Sentry native support on Android.
    /// </summary>
    public static class SentryNativeAndroid
    {
        private static IJniExecutor JniExecutor = new JniExecutor();

        /// <summary>
        /// Configures the native Android support.
        /// </summary>
        /// <param name="options">The Sentry Unity options to use.</param>
        public static void Configure(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo)
        {
            if (!options.AndroidNativeSupportEnabled)
            {
                return;
            }

            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            var sdkVersion = version.GetStatic<int>("SDK_INT");
            if (sdkVersion <= 23)
            {
                JniExecutor = new JniExecutorWithBackgroundWorker();
            }

            options.NativeContextWriter = new NativeContextWriter(JniExecutor);
            options.ScopeObserver = new AndroidJavaScopeObserver(options, JniExecutor);
            options.EnableScopeSync = true;
            options.CrashedLastRun = () =>
            {
                var crashedLastRun = SentryJava.CrashedLastRun(JniExecutor);
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

            try
            {
                options.DiagnosticLogger?.LogDebug("Reinstalling native backend.");

                // At this point Unity has taken the signal handler and will not invoke the original handler (Sentry)
                // So we register our backend once more to make sure user-defined data is available in the crash report.
                SentryNative.ReinstallBackend();
            }
            catch (Exception e)
            {
                options.DiagnosticLogger?.LogError(
                    e, "Failed to reinstall backend. Captured native crashes will miss scope data and tag.");
            }

            options.NativeSupportCloseCallback = () => Close(options.DiagnosticLogger);
            options.DefaultUserId = SentryJava.GetInstallationId(JniExecutor);
        }

        /// <summary>
        /// Closes the native Android support.
        /// </summary>
        public static void Close(IDiagnosticLogger? logger = null)
        {
            // Sentry Native is initialized and closed by the Java SDK, no need to call into it directly
            logger?.LogDebug("Closing the sentry-java SDK");
            SentryJava.Close(JniExecutor);

            JniExecutor.Dispose();
        }
    }
}
