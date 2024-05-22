using System;
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;
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
        /// <param name="sentryUnityInfo">Infos about the current Unity environment</param>
        public static void Configure(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo) =>
            Configure(options, sentryUnityInfo, ApplicationAdapter.Instance.Platform);

        internal static void Configure(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo, RuntimePlatform platform)
        {
            options.DiagnosticLogger?.LogInfo("Attempting to configure native support via the Cocoa SDK");

            if (!sentryUnityInfo.IsNativeSupportEnabled(options, platform))
            {
                options.DiagnosticLogger?.LogDebug("Native support is not enabled for: '{0}'", platform);
                return;
            }

            if (platform == RuntimePlatform.IPhonePlayer)
            {
                options.ScopeObserver = new NativeScopeObserver("iOS", options);
            }
            else
            {
                if (!SentryCocoaBridgeProxy.Init(options))
                {
                    options.DiagnosticLogger?.LogWarning("Failed to initialize the native SDK");
                    return;
                }
                options.ScopeObserver = new NativeScopeObserver("macOS", options);
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
