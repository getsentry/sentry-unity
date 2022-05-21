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
            // This is only provided on a best-effort basis for other then the explicitly supported platforms.
            if (options.BackgroundWorker is null)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Debug,
                        "Configuring on an unknown platform. Using WebBackgroundWorker to improve compatibility.");
                options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
            }
            options.DefaultUserId = AnalyticsSessionInfo.userId;
        }
    }
}
