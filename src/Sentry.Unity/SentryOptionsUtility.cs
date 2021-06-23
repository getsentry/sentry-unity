using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(SentryUnityOptions options, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            options.Enabled = true;
            options.Dsn = null;
            options.AutoSessionTracking = false;
            options.CaptureInEditor = true;
            options.RequestBodyCompressionLevel = CompressionLevelWithAuto.NoCompression;
            options.AttachStacktrace = false;

            options.StackTraceMode = StackTraceMode.Original;
            options.SampleRate = null;
            options.IsEnvironmentUser = false;

            options.Release = Release(application);
            options.Environment = Environment(application);

            options.CacheDirectoryPath = application.PersistentDataPath;

            options.Debug = true;
            options.DebugOnlyInEditor = true;
            options.DiagnosticLevel = SentryLevel.Warning;

            TryAttachLogger(options, application);
        }

        public static void SetDefaults(ScriptableSentryUnityOptions options)
        {
            options.Enabled = true;
            options.Dsn = string.Empty;
            options.CaptureInEditor = true;
            options.AttachStacktrace = false;
            options.SampleRate = 1.0f;

            options.ReleaseOverride = string.Empty;
            options.EnvironmentOverride = string.Empty;

            options.EnableOfflineCaching = true;

            options.Debug = true;
            options.DebugOnlyInEditor = true;
            options.DiagnosticLevel = SentryLevel.Warning;
        }

        private static string Release(IApplication application) =>
            application.ProductName is string productName
            && !string.IsNullOrWhiteSpace(productName)
                ? $"{productName}@{application.Version}"
                : $"{application.Version}";

        private static string Environment(IApplication application) => application.IsEditor ? "editor" : "production";

        private static void TryAttachLogger(SentryUnityOptions options, IApplication application)
        {
            if (options.DiagnosticLogger is null
                && options.Debug
                && (!options.DebugOnlyInEditor || application.IsEditor))
            {
                options.DiagnosticLogger = new UnityLogger(options);
            }
        }
    }
}
