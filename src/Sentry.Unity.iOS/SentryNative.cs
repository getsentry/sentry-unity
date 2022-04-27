using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.iOS
{
    /// <summary>
    /// Access to the Sentry native support on iOS/macOS.
    /// </summary>
    public static class SentryNativeCocoa
    {
        /// <summary>
        /// Configures the native support.
        /// </summary>
        /// <param name="options">The Sentry Unity options to use.</param>
        public static void Configure(SentryUnityOptions options)
        {
            if (options.IosNativeSupportEnabled)
            {
                options.ScopeObserver = new NativeScopeObserver("iOS", options);
                options.EnableScopeSync = true;
                options.CrashedLastRun = () =>
                {
                    var crashedLastRun = SentryCocoaBridgeProxy.CrashedLastRun() == 1;
                    options.DiagnosticLogger?
                        .LogDebug("Native iOS SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);

                    return crashedLastRun;
                };
                ApplicationAdapter.Instance.Quitting += () =>
                {
                    options.DiagnosticLogger?.LogDebug("Closing the sentry-cocoa SDK");
                    SentryCocoaBridgeProxy.Close();
                };
            }
        }
    }
}
