using System.Runtime.InteropServices;
using Sentry.Extensibility;

namespace Sentry.Unity
{
    public class UnityNativeScopeObserver : IScopeObserver
    {
        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeAddBreadcrumb(string timestamp, string? message, string? type, string? category, int level);
        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeAddExtra(string key);
        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeSetTag(string key, string value);
        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeUnsetTag(string key);
        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeSetUser(string? email, string? userId, string? ipAddress, string? username);
        [DllImport("__Internal")]
        private static extern void SentryNativeBridgeUnsetUser();

        private SentryUnityOptions _options;

        public UnityNativeScopeObserver(SentryUnityOptions options)
        {
            _options = options;
        }

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
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

            SentryNativeBridgeAddBreadcrumb(timestamp, breadcrumb.Message, breadcrumb.Type, breadcrumb.Category, level);
        }

        public void SetExtra(string key, object? value)
        {
            SentryNativeBridgeAddExtra(key);
        }

        public void SetTag(string key, string value)
        {
            _options.DiagnosticLogger?.LogDebug("To native bridge: Setting Tag");
            SentryNativeBridgeSetTag(key, value);
        }

        public void UnsetTag(string key)
        {
            _options.DiagnosticLogger?.LogDebug("To native bridge: Unsetting Tag");
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
                _options.DiagnosticLogger?.LogDebug("To native bridge: Setting User");
                SentryNativeBridgeSetUser(user.Email, user.Id, user.IpAddress, user.Username);
            }
        }
    }
}
