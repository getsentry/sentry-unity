using System.Runtime.InteropServices;

namespace Sentry.Unity.NativeUtils
{
    internal static class C
    {
        internal static void SetValueIfNotNull(sentry_value_t obj, string key, string? value)
        {
            if (value is not null)
            {
                _ = sentry_value_set_by_key(obj, key, sentry_value_new_string(value));
            }
        }

        internal static void SetValueIfNotNull(sentry_value_t obj, string key, int? value)
        {
            if (value.HasValue)
            {
                _ = sentry_value_set_by_key(obj, key, sentry_value_new_int32(value.Value));
            }
        }

        internal static void SetValueIfNotNull(sentry_value_t obj, string key, bool? value)
        {
            if (value.HasValue)
            {
                _ = sentry_value_set_by_key(obj, key, sentry_value_new_bool(value.Value ? 1 : 0));
            }
        }

        internal static void SetValueIfNotNull(sentry_value_t obj, string key, double? value)
        {
            if (value.HasValue)
            {
                _ = sentry_value_set_by_key(obj, key, sentry_value_new_double(value.Value));
            }
        }

        [DllImport("sentry")]
        internal static extern sentry_value_t sentry_value_new_object();

        [DllImport("sentry")]
        internal static extern sentry_value_t sentry_value_new_null();

        [DllImport("sentry")]
        internal static extern sentry_value_t sentry_value_new_bool(int value);

        [DllImport("sentry")]
        internal static extern sentry_value_t sentry_value_new_double(double value);

        [DllImport("sentry")]
        internal static extern sentry_value_t sentry_value_new_int32(int value);

        [DllImport("sentry")]
        internal static extern sentry_value_t sentry_value_new_string(string value);

        [DllImport("sentry")]
        internal static extern sentry_value_t sentry_value_new_breadcrumb(string? type, string? message);

        [DllImport("sentry")]
        internal static extern int sentry_value_set_by_key(sentry_value_t value, string k, sentry_value_t v);

        [DllImport("sentry")]
        internal static extern void sentry_set_context(string key, sentry_value_t value);

        [DllImport("sentry")]
        internal static extern void sentry_add_breadcrumb(sentry_value_t breadcrumb);

        [DllImport("sentry")]
        internal static extern void sentry_set_tag(string key, string value);

        [DllImport("sentry")]
        internal static extern void sentry_remove_tag(string key);

        [DllImport("sentry")]
        internal static extern void sentry_set_user(sentry_value_t user);

        [DllImport("sentry")]
        internal static extern void sentry_remove_user();

        [DllImport("sentry")]
        internal static extern void sentry_set_extra(string key, sentry_value_t value);

        [DllImport("sentry")]
        internal static extern void sentry_remove_extra(string key);

        // native union sentry_value_u/t
        [StructLayout(LayoutKind.Explicit)]
        internal struct sentry_value_t
        {
            [FieldOffset(0)]
            internal ulong _bits;
            [FieldOffset(0)]
            internal double _double;
        }

    }
}
