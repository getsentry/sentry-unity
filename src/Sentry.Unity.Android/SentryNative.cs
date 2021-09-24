using System.Runtime.InteropServices;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// P/Invoke to `sentry-native` functions.
    /// </summary>
    public static class SentryNative
    {
        /// <summary>
        /// Re-installs the sentry-native backend essentially retaking the signal handlers.
        /// </summary>
        /// <summary>
        /// Sentry's Android SDK initializes before Unity. But once Unity initializes, it takes the signal handler
        /// and does not forward the call to the original handler (sentry) before shutting down.
        /// This results in a crash captured by the Sentry Android SDK (Java/ART) layer captured crash report
        /// containing a tompstone, without any of the scope data such as tags set through
        /// Sentry SDKs C# -> Java -> C
        /// </summary>
		public static void ReinstallBackend() => sentry_reinstall_backend();

        // libsentry.io
        [DllImport("sentry")]
        private static extern void sentry_reinstall_backend();
    }
}
