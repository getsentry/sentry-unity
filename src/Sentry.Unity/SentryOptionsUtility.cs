using System.Linq;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(SentryUnityOptions options, IApplication? application = null,
            bool isBuilding = false)
        {
            application ??= ApplicationAdapter.Instance;

            options.Enabled = true;
            options.IsGlobalModeEnabled = true;

            options.AutoSessionTracking = true;
            options.CaptureInEditor = true;
            options.RequestBodyCompressionLevel = CompressionLevelWithAuto.NoCompression;
            options.InitCacheFlushTimeout = System.TimeSpan.Zero;

            options.StackTraceMode = StackTraceMode.Original;
            options.IsEnvironmentUser = false;

            options.Release = Release(application);
            options.Environment = Environment(application, isBuilding);

            options.CacheDirectoryPath = application.PersistentDataPath;
        }

        public static void SetDefaults(ScriptableSentryUnityOptions scriptableOptions)
        {
            var options = new SentryUnityOptions();
            SetDefaults(options);

            scriptableOptions.Enabled = options.Enabled;

            scriptableOptions.Dsn = options.Dsn;
            scriptableOptions.CaptureInEditor = options.CaptureInEditor;
            scriptableOptions.TracesSampleRate = options.TracesSampleRate;
            scriptableOptions.AutoSessionTrackingInterval = (int)options.AutoSessionTrackingInterval.TotalMilliseconds;
            scriptableOptions.AutoSessionTracking = options.AutoSessionTracking;

            scriptableOptions.AttachStacktrace = options.AttachStacktrace;
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

            scriptableOptions.Debug = true;
            scriptableOptions.DebugOnlyInEditor = true;
            scriptableOptions.DiagnosticLevel = SentryLevel.Warning;
        }

        private static string Release(IApplication application)
        {
            if (application.ProductName is string productName
                && !string.IsNullOrWhiteSpace(productName)
                && productName.Any(c => c != '.')) // productName consisting solely of '.'
            {
                productName = Regex.Replace(productName, @"\n|\r|\t|\/|\\|\.{2}|@", "_");
                return $"{productName}@{application.Version}";
            }

            return application.Version;
        }

        private static string Environment(IApplication application, bool isBuilding) => (application.IsEditor && !isBuilding) ? "editor" : "production";

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
