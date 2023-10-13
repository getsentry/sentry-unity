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
        public static void Configure(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo) =>
            Configure(options, sentryUnityInfo, ApplicationAdapter.Instance.Platform);

        internal static void Configure(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo, RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    if (!options.IosNativeSupportEnabled)
                    {
                        return;
                    }
                    options.ScopeObserver = new NativeScopeObserver("iOS", options);
                    break;
                case RuntimePlatform.OSXPlayer or RuntimePlatform.OSXServer:
                    if (!options.MacosNativeSupportEnabled)
                    {
                        return;
                    }
                    if (!SentryCocoaBridgeProxy.Init(options))
                    {
                        options.DiagnosticLogger?.LogWarning("Failed to initialize the native SDK");
                        return;
                    }
                    options.ScopeObserver = new NativeScopeObserver("macOS", options);
                    break;
                default:
                    options.DiagnosticLogger?
                        .LogWarning("Cocoa SentryNative.Configure() called for unsupported platform: '{0}'", platform);
                    return;
            }

            options.NativeContextWriter = new NativeContextWriter();
            options.EnableScopeSync = true;
            options.CrashedLastRun = () =>
            {
                var crashedLastRun = SentryCocoaBridgeProxy.CrashedLastRun() == 1;
                options.DiagnosticLogger?
                    .LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);

                return crashedLastRun;
            };

            options.NativeSupportCloseCallback += () => Close(options.DiagnosticLogger);
            if (sentryUnityInfo.IL2CPP)
            {
                options.DefaultUserId = SentryCocoaBridgeProxy.GetInstallationId();
            }
        }

        /// <summary>
        /// Closes the native Cocoa support.
        /// </summary>
        public static void Close(IDiagnosticLogger? logger = null)
        {
            logger?.LogDebug("Closing the sentry-cocoa SDK");
            SentryCocoaBridgeProxy.Close();
        }
    }
}
