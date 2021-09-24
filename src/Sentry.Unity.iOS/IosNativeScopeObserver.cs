using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Unity.Json;

namespace Sentry.Unity.iOS
{
    public class IosNativeScopeObserver : IScopeObserver
    {
        private readonly SentryUnityOptions _options;

        public IosNativeScopeObserver(SentryUnityOptions options)
        {
            _options = options;
            _options.CrashedLastRun = SentryCocoaBridgeProxy.CrashedLastRun;
        }

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            var timestamp = GetTimestamp(breadcrumb.Timestamp);
            var level = GetBreadcrumbLevel(breadcrumb.Level);

            _options.DiagnosticLogger?.LogDebug("Native bridge - Adding breadcrumb m:\"{0}\" l:\"{1}\"", breadcrumb.Message, level);
            SentryCocoaBridgeProxy.SentryNativeBridgeAddBreadcrumb(timestamp, breadcrumb.Message, breadcrumb.Type, breadcrumb.Category, level);
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
                extraValue = SafeSerializer.SerializeSafely(value);
                if (extraValue is null)
                {
                    return;
                }
            }

            _options.DiagnosticLogger?.LogDebug("Native bridge - Setting Extra k:\"{0}\" v:\"{1}\"", key, value);
            SentryCocoaBridgeProxy.SentryNativeBridgeSetExtra(key, extraValue);
        }

        public void SetTag(string key, string value)
        {
            _options.DiagnosticLogger?.LogDebug("Native bridge - Setting Tag k:\"{0}\" v:\"{1}\"", key, value);
            SentryCocoaBridgeProxy.SentryNativeBridgeSetTag(key, value);
        }

        public void UnsetTag(string key)
        {
            _options.DiagnosticLogger?.LogDebug("Native bridge - Unsetting Tag k:\"{0}\"", key);
            SentryCocoaBridgeProxy.SentryNativeBridgeUnsetTag(key);
        }

        public void SetUser(User? user)
        {
            if (user is null)
            {
                _options.DiagnosticLogger?.LogDebug("Native bridge - Unsetting User");
                SentryCocoaBridgeProxy.SentryNativeBridgeUnsetUser();
            }
            else
            {
                _options.DiagnosticLogger?.LogDebug("Native bridge - Setting User i:\"{0}\" n:\"{1}\"", user.Id, user.Username);
                SentryCocoaBridgeProxy.SentryNativeBridgeSetUser(user.Email, user.Id, user.IpAddress, user.Username);
            }
        }
    }
}
