using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

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
            switch (ApplicationAdapter.Instance.Platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    if (!options.IosNativeSupportEnabled)
                    {
                        return;
                    }
                    options.ScopeObserver = new NativeScopeObserver("iOS", options);
                    break;
                case RuntimePlatform.OSXPlayer:
                    if (!options.MacosNativeSupportEnabled)
                    {
                        return;
                    }
                    if (!SentryCocoaBridgeProxy.Init(options))
                    {
                        options.DiagnosticLogger?.LogWarning("Failed to initialzie the native SDK");
                        return;
                    }
                    options.ScopeObserver = new NativeScopeObserver("macOS", options);
                    break;
            }

            options.EnableScopeSync = true;
            options.CrashedLastRun = () =>
            {
                var crashedLastRun = SentryCocoaBridgeProxy.CrashedLastRun() == 1;
                options.DiagnosticLogger?
                    .LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);

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
