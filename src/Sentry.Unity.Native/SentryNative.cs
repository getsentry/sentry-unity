using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using System.Collections.Generic;

namespace Sentry.Unity.Native
{
    /// <summary>
    /// Access to the Sentry native support of the sentry-native SDK.
    /// </summary>
    public static class SentryNative
    {
        private static Dictionary<string, bool> _perDirectoryCrashInfo = new Dictionary<string, bool>();

        /// <summary>
        /// Configures the native SDK.
        /// </summary>
        /// <param name="options">The Sentry Unity options to use.</param>
        public static void Configure(SentryUnityOptions options)
        {
            if (options.WindowsNativeSupportEnabled)
            {
                SentryNativeBridge.Init(options);
                ApplicationAdapter.Instance.Quitting += () =>
                {
                    options.DiagnosticLogger?.LogDebug("Closing the sentry-native SDK");
                    SentryNativeBridge.Close();
                };
                options.ScopeObserver = new NativeScopeObserver(options);
                options.EnableScopeSync = true;

                // Note: we must actually call the function now and on every other call use the value we get here.
                // Additionally, we cannot call this multiple times for the same directory, because the result changes
                // on subsequent runs. Therefore, we cache the value during the whole runtime of the application.
                var cacheDirectory = SentryNativeBridge.GetCacheDirectory(options);
                bool crashedLastRun = false;
                // In the event the SDK is re-initialized with a different path on disk, we need to track which ones were already read
                // Similarly we need to cache the value of each call since a subsequent call would return a different value
                // as the file used on disk to mark it as crashed is deleted after we read it.
                lock (_perDirectoryCrashInfo)
                {
                    if (!_perDirectoryCrashInfo.TryGetValue(cacheDirectory, out crashedLastRun))
                    {
                        crashedLastRun = SentryNativeBridge.HandleCrashedLastRun(options);
                        _perDirectoryCrashInfo.Add(cacheDirectory, crashedLastRun);

                        options.DiagnosticLogger?
                            .LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);
                    }
                }
                options.CrashedLastRun = () => crashedLastRun;

                // At this point Unity has taken the signal handler and will not invoke the original handler (Sentry)
                // So we register our backend once more to make sure user-defined data is available in the crash report.
                SentryNativeBridge.ReinstallBackend();
            }
        }
    }
}
