using System;

namespace Sentry.Unity.iOS
{
    public class IosNativeScopeObserver : IScopeObserver
    {
        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            var level = GetBreadcrumbLevel(breadcrumb.Level);
            var timestamp = GetTimestamp(breadcrumb.Timestamp);

            SentryCocoaBridgeProxy.SentryNativeBridgeAddBreadcrumb(timestamp, breadcrumb.Message, breadcrumb.Type, breadcrumb.Category, level);
        }

        public void SetExtra(string key, string value) => SentryCocoaBridgeProxy.SentryNativeBridgeSetExtra(key, value);

        public void UnsetExtra(string key) => SentryCocoaBridgeProxy.SentryNativeBridgeSetExtra(key, null);

        public void SetTag(string key, string value) => SentryCocoaBridgeProxy.SentryNativeBridgeSetTag(key, value);

        public void UnsetTag(string key) => SentryCocoaBridgeProxy.SentryNativeBridgeUnsetTag(key);

        public void SetUser(User user) =>
                SentryCocoaBridgeProxy.SentryNativeBridgeSetUser(user.Email, user.Id, user.IpAddress, user.Username);

        public void UnsetUser() =>
                SentryCocoaBridgeProxy.SentryNativeBridgeUnsetUser();

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
    }
}
