using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;

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
    public class SentryNativeBridge : IScopeObserver
    {
        public static void Init(SentryUnityOptions options)
        {
            var cOptions = sentry_options_new();

            // Note: DSN is not null because options.Validate() must have returned true for this to be called.
            sentry_options_set_dsn(cOptions, options.Dsn!);

            if (options.Release is not null)
            {
                options.DiagnosticLogger?.LogDebug("Setting Release: {0}", options.Release);
                sentry_options_set_release(cOptions, options.Release);
            }

            sentry_init(cOptions);
        }

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            // see https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/
            var crumb = sentry_value_new_breadcrumb(breadcrumb.Type, breadcrumb.Message);
            sentry_value_set_by_key(crumb, "level", sentry_value_new_string(breadcrumb.Level.ToString().ToLower()));
            sentry_value_set_by_key(crumb, "timestamp", sentry_value_new_string(GetTimestamp(breadcrumb.Timestamp)));
            nativeSetValueIfNotNull(crumb, "category", breadcrumb.Category);
            sentry_add_breadcrumb(crumb);
        }

        public void SetExtra(string key, string value) => sentry_set_extra(key, sentry_value_new_string(value));

        public void UnsetExtra(string key) => sentry_remove_extra(key);

        public void SetTag(string key, string value) => sentry_set_tag(key, value);

        public void UnsetTag(string key) => sentry_remove_tag(key);

        public void SetUser(User user)
        {
            // see https://develop.sentry.dev/sdk/event-payloads/user/
            var cUser = sentry_value_new_object();
            nativeSetValueIfNotNull(cUser, "id", user.Id);
            nativeSetValueIfNotNull(cUser, "username", user.Username);
            nativeSetValueIfNotNull(cUser, "email", user.Email);
            nativeSetValueIfNotNull(cUser, "ip_address", user.IpAddress);
            sentry_set_user(cUser);
        }

        public void UnsetUser() => sentry_remove_user();

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

        [DllImport("sentry")]
        private static extern SentryValueU sentry_value_new_object();

        [DllImport("sentry")]
        private static extern SentryValueU sentry_value_new_string(string value);

        [DllImport("sentry")]
        private static extern SentryValueU sentry_value_new_breadcrumb(string? type, string? message);

        [DllImport("sentry")]
        private static extern int sentry_value_set_by_key(SentryValueU value, string k, SentryValueU v);

        private static void nativeSetValueIfNotNull(SentryValueU obj, string key, string? value)
        {
            if (value is not null)
            {
                sentry_value_set_by_key(obj, key, sentry_value_new_string(value));
            }
        }

        [DllImport("sentry")]
        private static extern void sentry_add_breadcrumb(SentryValueU breadcrumb);

        [DllImport("sentry")]
        private static extern void sentry_set_tag(string key, string value);

        [DllImport("sentry")]
        private static extern void sentry_remove_tag(string key);

        [DllImport("sentry")]
        private static extern void sentry_set_user(SentryValueU user);

        [DllImport("sentry")]
        private static extern void sentry_remove_user();

        [DllImport("sentry")]
        private static extern void sentry_set_extra(string key, SentryValueU value);

        [DllImport("sentry")]
        private static extern void sentry_remove_extra(string key);

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

        // native union sentry_value_u/t
        [StructLayout(LayoutKind.Explicit)]
        private struct SentryValueU
        {
            [FieldOffset(0)]
            private ulong _bits;
            [FieldOffset(0)]
            private double _double;
        }

        private static string GetTimestamp(DateTimeOffset timestamp) =>
            // "o": Using ISO 8601 to make sure the timestamp makes it to the bridge correctly.
            // https://docs.microsoft.com/en-gb/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
            timestamp.ToString("o");
    }
}
