using System.Linq;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(ScriptableSentryUnityOptions scriptableOptions)
        {
            var options = new SentryUnityOptions();

            scriptableOptions.Enabled = options.Enabled;

            scriptableOptions.Dsn = options.Dsn;
            scriptableOptions.CaptureInEditor = options.CaptureInEditor;
            scriptableOptions.TracesSampleRate = options.TracesSampleRate;
            scriptableOptions.AutoSessionTrackingInterval = (int)options.AutoSessionTrackingInterval.TotalMilliseconds;
            scriptableOptions.AutoSessionTracking = options.AutoSessionTracking;

            scriptableOptions.AttachStacktrace = options.AttachStacktrace;
            scriptableOptions.AttachScreenshot = options.AttachScreenshot;
            scriptableOptions.ScreenshotMaxWidth = options.ScreenshotMaxWidth;
            scriptableOptions.ScreenshotMaxHeight = options.ScreenshotMaxHeight;
            scriptableOptions.ScreenshotQuality = options.ScreenshotQuality;
            scriptableOptions.MaxBreadcrumbs = options.MaxBreadcrumbs;
            scriptableOptions.ReportAssembliesMode = options.ReportAssembliesMode;
            scriptableOptions.SendDefaultPii = options.SendDefaultPii;
            scriptableOptions.IsEnvironmentUser = options.IsEnvironmentUser;

            scriptableOptions.MaxCacheItems = options.MaxCacheItems;
            scriptableOptions.InitCacheFlushTimeout = (int)options.InitCacheFlushTimeout.TotalMilliseconds;
            scriptableOptions.SampleRate = options.SampleRate;
            scriptableOptions.ShutdownTimeout = (int)options.ShutdownTimeout.TotalMilliseconds;
            scriptableOptions.MaxQueueItems = options.MaxQueueItems;

            // Config window specifics
            scriptableOptions.ReleaseOverride = string.Empty;
            scriptableOptions.EnvironmentOverride = string.Empty;

            scriptableOptions.EnableOfflineCaching = true;

            scriptableOptions.IosNativeSupportEnabled = options.IosNativeSupportEnabled;
            scriptableOptions.AndroidNativeSupportEnabled = options.AndroidNativeSupportEnabled;
            scriptableOptions.WindowsNativeSupportEnabled = options.WindowsNativeSupportEnabled;

            scriptableOptions.Debug = true;
            scriptableOptions.DebugOnlyInEditor = true;
            scriptableOptions.DiagnosticLevel = SentryLevel.Warning;
        }

        public static void TryAttachLogger(SentryUnityOptions options, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            if (options.DiagnosticLogger is null
                && options.Debug
                && (!options.DebugOnlyInEditor || application.IsEditor))
            {
                options.DiagnosticLogger = new UnityLogger(options);
            }
        }
    }
}
