using System;
using System.Runtime.InteropServices;

namespace Sentry.Unity
{
    /// <summary>
    /// P/Invoke to `sentry-native` functions.
    /// </summary>
    /// <remarks>
    /// The `sentry-native` SDK on Android is brought in through the `sentry-android-ndk`
    /// maven package.
    /// On Standalone players on Windows and Linux it's build directly for those platforms.
    /// </remarks>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    /// <see href="https://github.com/getsentry/sentry-native"/>
    public static class SentryNativeBridge
    {
        // TODO:
        //   sentry_options_t *options = sentry_options_new();
        //   sentry_options_set_dsn(options, "https://examplePublicKey@o0.ingest.sentry.io/0");
        //   sentry_options_set_release(options, "my-project-name@2.3.12");
        //   sentry_init(options);

        //
        public static void Init(string dsn, string release)
        {
            var options = sentry_options_new();
            sentry_options_set_dsn(options, dsn);
            sentry_options_set_release(options, release);
            sentry_init(options);
        }

        // libsentry.so
        [DllImport("sentry")]
        private static extern IntPtr sentry_options_new();

        [DllImport("sentry")]
        private static extern void sentry_options_set_dsn(IntPtr options, string dsn);

        [DllImport("sentry")]
        private static extern void sentry_options_set_release(IntPtr options, string release);

        // TODO we could set a logger for sentry-native, forwarding the logs to `options.DiagnosticLogger?`
        // [DllImport("sentry")]
        // private static extern void sentry_options_set_logger(IntPtr options, IntPtr logger, IntPtr userData);

        [DllImport("sentry")]
        private static extern void sentry_init(IntPtr options);

        /// <summary>
        /// Re-installs the sentry-native backend essentially retaking the signal handlers.
        /// </summary>
        /// <summary>
        /// Sentry's Android SDK initializes before Unity. But once Unity initializes, it takes the signal handler
        /// and does not forward the call to the original handler (sentry) before shutting down.
        /// This results in a crash captured by the Sentry Android SDK (Java/ART) layer captured crash report
        /// containing a tombstone, without any of the scope data such as tags set through
        /// Sentry SDKs C# -> Java -> C
        /// </summary>
        public static void ReinstallBackend() => ReinstallSentryNativeBackendStrategy();

        // libsentry.so
        [DllImport("sentry")]
        private static extern void sentry_reinstall_backend();

        // Testing
        internal static Action ReinstallSentryNativeBackendStrategy = sentry_reinstall_backend;
    }
}
