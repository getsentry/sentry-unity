using Sentry.Extensibility;
using UnityEngine.Analytics;

namespace Sentry.Unity.Default
{
    /// <summary>
    /// Configure Sentry on an officially unsupported platform.
    /// This works on a best-effort basis, to try to improve compatibility.
    /// </summary>
    public static class SentryUnknownPlatform
    {
        public static void Configure(SentryUnityOptions options)
        {
            // This is only provided on a best-effort basis for other than the explicitly supported platforms.
            if (options.BackgroundWorker is null)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Platform support for background thread execution is unknown: using WebBackgroundWorker.");
                options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
            }

            options.DefaultUserId = AnalyticsSessionInfo.userId;

            if (options.CacheDirectoryPath is not null)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Platform support for offline caching is unknown - disabling it.");
                options.CacheDirectoryPath = null;
            }

            // Requires file access, see https://github.com/getsentry/sentry-unity/issues/290#issuecomment-1163608988
            if (options.AutoSessionTracking)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Platform support for automatic session tracking is unknown: disabling.");
                options.AutoSessionTracking = false;
            }
        }
    }
}
