using Sentry.Extensibility;

namespace Sentry.Unity.iOS
{
    /// <summary>
    /// Access to the Sentry native support on iOS.
    /// </summary>
    public static class SentryNativeIos
    {
        /// <summary>
        /// Configures the native Android support.
        /// </summary>
        /// <param name="options">The Sentry Unity options to use.</param>
        public static void Configure(SentryUnityOptions options)
        {
            if (options.IosNativeSupportEnabled)
            {
                options.ScopeObserver = new IosNativeScopeObserver(options);
                options.EnableScopeSync = true;
                options.CrashedLastRun = () =>
                {
                    var crashedLastRun = SentryCocoaBridgeProxy.CrashedLastRun();
                    options.DiagnosticLogger?
                        .LogDebug("Native iOS SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);

                    return crashedLastRun;
                };
            }
        }
    }
}
