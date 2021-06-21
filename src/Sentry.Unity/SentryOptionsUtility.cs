using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(SentryUnityOptions options, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            options.Enabled = Enabled;
            options.Dsn = string.Empty;
            options.CaptureInEditor = CaptureInEditor;
            options.RequestBodyCompressionLevel = RequestBodyCompressionLevel;
            options.AttachStacktrace = AttackStackTrace;
            options.StackTraceMode = StackTraceMode;
            options.SampleRate = SampleRate;
            options.IsEnvironmentUser = IsEnvironmentUser;

            options.Release = Release(application);
            options.Environment = Environment(application);

            options.CacheDirectoryPath = application.PersistentDataPath;

            options.Debug = Debug;
            options.DebugOnlyInEditor = DebugOnlyInEditor;
            options.DiagnosticLevel = DiagnosticLevel;

            TryAttachLogger(options, application);
        }

        public static void SetDefaults(ScriptableSentryUnityOptions options)
        {
            options.Enabled = Enabled;
            options.Dsn = string.Empty;
            options.CaptureInEditor = CaptureInEditor;
            options.Debug = Debug;
            options.DiagnosticLevel = DiagnosticLevel;
            options.AttachStacktrace = AttackStackTrace;
            options.SampleRate = SampleRate;

            options.ReleaseOverride = string.Empty;
            options.EnvironmentOverride = string.Empty;

            options.EnableOfflineCaching = true;

            options.Debug = Debug;
            options.DebugOnlyInEditor = DebugOnlyInEditor;
            options.DiagnosticLevel = DiagnosticLevel;
        }

        private static bool Enabled = true;
        private static bool CaptureInEditor = true;

        // 'Optimal' and 'Fastest' don't work on IL2CPP. Forcing 'NoCompression'.
        private static CompressionLevelWithAuto RequestBodyCompressionLevel = CompressionLevelWithAuto.NoCompression;

        private static bool AttackStackTrace = false;
        private static StackTraceMode StackTraceMode = default;
        private static float SampleRate = 1.0f;
        private static bool IsEnvironmentUser = false;

        private static string Release(IApplication application)
        {
            return application.ProductName is string productName
                && !string.IsNullOrWhiteSpace(productName)
                    ? $"{productName}@{application.Version}"
                    : $"{application.Version}";
        }

        private static string Environment(IApplication application)
        {
            return application.IsEditor ? "editor" : "production";
        }

        private static bool Debug = true;
        private static bool DebugOnlyInEditor = true;
        private static SentryLevel DiagnosticLevel = SentryLevel.Warning;

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
