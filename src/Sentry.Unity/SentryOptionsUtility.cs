using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(SentryUnityOptions options, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            // 'Optimal' and 'Fastest' don't work on IL2CPP. Forcing 'NoCompression'.
            options.RequestBodyCompressionLevel = CompressionLevelWithAuto.NoCompression;
            options.IsEnvironmentUser = false;

            SetRelease(options, application);
            SetEnvironment(options, application);
            SetCacheDirectoryPath(options, application);
        }

        private static void SetRelease(SentryUnityOptions options, IApplication application)
        {
            options.Release ??= application.ProductName is string productName
                && !string.IsNullOrWhiteSpace(productName)
                    ? $"{productName}@{application.Version}"
                    : $"{application.Version}";

            options.DiagnosticLogger?.LogDebug("option.Release: {0}", options.Release);
        }

        private static void SetEnvironment(SentryUnityOptions options, IApplication application)
        {
            options.Environment ??= application.IsEditor ? "editor" : "production";
            options.DiagnosticLogger?.LogDebug("option.Environment: {0}", options.Environment);
        }

        private static void SetCacheDirectoryPath(SentryUnityOptions options, IApplication application)
        {
            options.CacheDirectoryPath ??= application.PersistentDataPath;
            options.DiagnosticLogger?.LogDebug("option.CacheDirectoryPath: {0}", options.CacheDirectoryPath);
        }
    }
}
