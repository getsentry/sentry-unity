using System.Runtime.InteropServices;

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
        [DllImport("__Internal", EntryPoint = "SentryNativeBridgeInit")]
        public static extern int Init();

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
