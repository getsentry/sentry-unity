using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;

namespace Sentry.Unity.iOS
{
    /// <summary>
    /// P/Invoke to SentryNativeBridge.m which communicates with the `sentry-cocoa` SDK.
    /// </summary>
    /// <remarks>
    /// Functions are declared in `SentryNativeBridge.m`
    /// </remarks>
    /// <see href="https://github.com/getsentry/sentry-cocoa"/>
    internal static class SentryCocoaBridgeProxy
    {
        public static bool Init(SentryUnityOptions options)
        {
            // Note: used on macOS only
            if (LoadLibrary() != 1)
            {
                return false;
            }

            var cOptions = OptionsNew();

            // Note: DSN is not null because options.IsValid() must have returned true for this to be called.
            OptionsSetString(cOptions, "dsn", options.Dsn!);

            if (options.Release is not null)
            {
                options.DiagnosticLogger?.LogDebug("Setting Release: {0}", options.Release);
                OptionsSetString(cOptions, "release", options.Release);
            }

            if (options.Environment is not null)
            {
                options.DiagnosticLogger?.LogDebug("Setting Environment: {0}", options.Environment);
                OptionsSetString(cOptions, "environment", options.Environment);
            }

            options.DiagnosticLogger?.LogDebug("Setting Debug: {0}", options.Debug);
            OptionsSetInt(cOptions, "debug", options.Debug ? 1 : 0);

            var diagnosticLevel = options.DiagnosticLevel.ToString().ToLowerInvariant();
            options.DiagnosticLogger?.LogDebug("Setting DiagnosticLevel: {0}", diagnosticLevel);
            OptionsSetString(cOptions, "diagnosticLevel", diagnosticLevel);

            // Disabling the native in favor of the C# layer for now
            options.DiagnosticLogger?.LogDebug("Disabling native auto session tracking");
            OptionsSetInt(cOptions, "enableAutoSessionTracking", 0);

            options.DiagnosticLogger?.LogDebug("Setting SendDefaultPii: {0}", options.SendDefaultPii);
            OptionsSetInt(cOptions, "sendDefaultPii", options.SendDefaultPii ? 1 : 0);

            options.DiagnosticLogger?.LogDebug("Setting MaxBreadcrumbs: {0}", options.MaxBreadcrumbs);
            OptionsSetInt(cOptions, "maxBreadcrumbs", options.MaxBreadcrumbs);

            options.DiagnosticLogger?.LogDebug("Setting MaxCacheItems: {0}", options.MaxCacheItems);
            OptionsSetInt(cOptions, "maxCacheItems", options.MaxCacheItems);

            StartWithOptions(cOptions);
            return true;
        }

        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeLoadLibrary")]
        private static extern int LoadLibrary();

        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsNew")]
        private static extern IntPtr OptionsNew();

        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsSetString")]
        private static extern void OptionsSetString(IntPtr options, string name, string value);

        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsSetInt")]
        private static extern void OptionsSetInt(IntPtr options, string name, int value);

        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeStartWithOptions")]
        private static extern void StartWithOptions(IntPtr options);

        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeCrashedLastRun")]
        public static extern int CrashedLastRun();

        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeClose")]
        public static extern void Close();

        [DllImport("__Internal")]
        public static extern void SentryNativeBridgeAddBreadcrumb(string timestamp, string? message, string? type, string? category, int level);

        [DllImport("__Internal")]
        public static extern void SentryNativeBridgeSetExtra(string key, string? value);

        [DllImport("__Internal")]
        public static extern void SentryNativeBridgeSetTag(string key, string value);

        [DllImport("__Internal")]
        public static extern void SentryNativeBridgeUnsetTag(string key);

        [DllImport("__Internal")]
        public static extern void SentryNativeBridgeSetUser(string? email, string? userId, string? ipAddress, string? username);

        [DllImport("__Internal")]
        public static extern void SentryNativeBridgeUnsetUser();
    }
}
