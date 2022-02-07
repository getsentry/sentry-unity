using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;

namespace Sentry.Unity
{
    /// <summary>
    /// Scope Observer for Native through P/Invoke.
    /// </summary>
    /// <see href="https://github.com/getsentry/sentry-native"/>
    public class NativeScopeObserver : ScopeObserver
    {
        public NativeScopeObserver(SentryOptions options) : base("Native", options) { }

        public override void AddBreadcrumbImpl(Breadcrumb breadcrumb)
        {
            // see https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/
            var crumb = sentry_value_new_breadcrumb(breadcrumb.Type, breadcrumb.Message);
            sentry_value_set_by_key(crumb, "level", sentry_value_new_string(breadcrumb.Level.ToString().ToLower()));
            sentry_value_set_by_key(crumb, "timestamp", sentry_value_new_string(GetTimestamp(breadcrumb.Timestamp)));
            nativeSetValueIfNotNull(crumb, "category", breadcrumb.Category);
            sentry_add_breadcrumb(crumb);
        }

        public override void SetExtraImpl(string key, string? value) =>
            sentry_set_extra(key, value is null ? sentry_value_new_null() : sentry_value_new_string(value));

        public override void SetTagImpl(string key, string value) => sentry_set_tag(key, value);

        public override void UnsetTagImpl(string key) => sentry_remove_tag(key);

        public override void SetUserImpl(User user)
        {
            // see https://develop.sentry.dev/sdk/event-payloads/user/
            var cUser = sentry_value_new_object();
            nativeSetValueIfNotNull(cUser, "id", user.Id);
            nativeSetValueIfNotNull(cUser, "username", user.Username);
            nativeSetValueIfNotNull(cUser, "email", user.Email);
            nativeSetValueIfNotNull(cUser, "ip_address", user.IpAddress);
            sentry_set_user(cUser);
        }

        public override void UnsetUserImpl() => sentry_remove_user();

        [DllImport("sentry")]
        private static extern SentryValueU sentry_value_new_object();

        [DllImport("sentry")]
        private static extern SentryValueU sentry_value_new_null();

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
