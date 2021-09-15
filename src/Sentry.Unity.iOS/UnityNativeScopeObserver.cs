using System.Runtime.InteropServices;
using Sentry.Extensibility;

namespace Sentry.Unity.iOS
{
    public class UnityNativeScopeObserver : IScopeObserver
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

        public UnityNativeScopeObserver(SentryUnityOptions options) => _options = options;

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            // "o": Using ISO 8601 to make sure the timestamp makes it to the bridge correctly.
            // https://docs.microsoft.com/en-gb/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
            var timestamp = breadcrumb.Timestamp.ToString("o");
            var level = breadcrumb.Level switch
            {
                BreadcrumbLevel.Debug => 0,
                BreadcrumbLevel.Info => 1,
                BreadcrumbLevel.Warning => 2,
                BreadcrumbLevel.Error => 3,
                BreadcrumbLevel.Critical => 4,
                _ => -1
            };

            _options.DiagnosticLogger?.LogDebug("To native bridge: Adding breadcrumb m:\"{0}\" l:\"{1}\"", breadcrumb.Message, level);
            SentryNativeBridgeAddBreadcrumb(timestamp, breadcrumb.Message, breadcrumb.Type, breadcrumb.Category, level);
        }

        public void SetExtra(string key, object? value)
        {
            _options.DiagnosticLogger?.LogDebug("To native bridge: Setting Extra k:\"{0}\" v:\"{1}\"", key, value);
            SentryNativeBridgeSetExtra(key, value is null ? null : System.Text.Json.JsonSerializer.Serialize(value));
        }

        public void SetTag(string key, string value)
        {
            _options.DiagnosticLogger?.LogDebug("To native bridge: Setting Tag k:\"{0}\" v:\"{1}\"", key, value);
            SentryNativeBridgeSetTag(key, value);
        }

        public void UnsetTag(string key)
        {
            _options.DiagnosticLogger?.LogDebug("To native bridge: Unsetting Tag k:\"{0}\"", key);
            SentryNativeBridgeUnsetTag(key);
        }

        public void SetUser(User? user)
        {
            if (user is null)
            {
                _options.DiagnosticLogger?.LogDebug("To native bridge: Unsetting User");
                SentryNativeBridgeUnsetUser();
            }
            else
            {
                _options.DiagnosticLogger?.LogDebug("To native bridge: Setting User i:\"{0}\" n:\"{1}\"", user.Id, user.Username);
                SentryNativeBridgeSetUser(user.Email, user.Id, user.IpAddress, user.Username);
            }
        }
    }
}
