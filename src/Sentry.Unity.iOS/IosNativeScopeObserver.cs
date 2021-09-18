using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;

namespace Sentry.Unity.iOS
{
    public class IosNativeScopeObserver : IScopeObserver
    {
        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeAddBreadcrumb(string timestamp, string? message, string? type, string? category, int level);

        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeSetExtra(string key, string? value);

        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeSetTag(string key, string value);

        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeUnsetTag(string key);

        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeSetUser(string? email, string? userId, string? ipAddress, string? username);

        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeUnsetUser();

        private readonly SentryUnityOptions _options;

        public IosNativeScopeObserver(SentryUnityOptions options) => _options = options;

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            var timestamp = GetTimestamp(breadcrumb.Timestamp);
            var level = GetBreadcrumbLevel(breadcrumb.Level);

            _options.DiagnosticLogger?.LogDebug("Native bridge - Adding breadcrumb m:\"{0}\" l:\"{1}\"", breadcrumb.Message, level);
            SentryNativeBridgeAddBreadcrumb(timestamp, breadcrumb.Message, breadcrumb.Type, breadcrumb.Category, level);
        }

        internal static string GetTimestamp(DateTimeOffset timestamp) =>
            // "o": Using ISO 8601 to make sure the timestamp makes it to the bridge correctly.
            // https://docs.microsoft.com/en-gb/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
            timestamp.ToString("o");

        internal static int GetBreadcrumbLevel(BreadcrumbLevel breadcrumbLevel) =>
            // https://github.com/getsentry/sentry-cocoa/blob/50f955aeb214601dd62b5dae7abdaddc8a1f24d9/Sources/Sentry/Public/SentryDefines.h#L99-L105
            breadcrumbLevel switch
            {
                BreadcrumbLevel.Debug => 1,
                BreadcrumbLevel.Info => 2,
                BreadcrumbLevel.Warning => 3,
                BreadcrumbLevel.Error => 4,
                BreadcrumbLevel.Critical => 5,
                _ => 0
            };

        public void SetExtra(string key, object? value)
        {
            string? extraValue = null;
            if (value is not null)
            {
                extraValue = SerializeExtraValue(value);
            }

            _options.DiagnosticLogger?.LogDebug("Native bridge - Setting Extra k:\"{0}\" v:\"{1}\"", key, value);
            SentryNativeBridgeSetExtra(key, extraValue);
        }

        internal string? SerializeExtraValue(object value)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(value);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Native bridge - Failed to serialize extra value of type \"{0}\"", e, value.GetType());
                return null;
            }
        }

        public void SetTag(string key, string value)
        {
            _options.DiagnosticLogger?.LogDebug("Native bridge - Setting Tag k:\"{0}\" v:\"{1}\"", key, value);
            SentryNativeBridgeSetTag(key, value);
        }

        public void UnsetTag(string key)
        {
            _options.DiagnosticLogger?.LogDebug("Native bridge - Unsetting Tag k:\"{0}\"", key);
            SentryNativeBridgeUnsetTag(key);
        }

        public void SetUser(User? user)
        {
            if (user is null)
            {
                _options.DiagnosticLogger?.LogDebug("Native bridge - Unsetting User");
                SentryNativeBridgeUnsetUser();
            }
            else
            {
                _options.DiagnosticLogger?.LogDebug("Native bridge - Setting User i:\"{0}\" n:\"{1}\"", user.Id, user.Username);
                SentryNativeBridgeSetUser(user.Email, user.Id, user.IpAddress, user.Username);
            }
        }
    }
}
