using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(SentryUnityOptions options, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

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

            Log(options.DiagnosticLogger, "Release", options.Release);
        }

        private static void SetEnvironment(SentryUnityOptions options, IApplication application)
        {
            options.Environment ??= application.IsEditor ? "editor" : "production";
            Log(options.DiagnosticLogger, "Environment", options.Environment);
        }

        private static void SetCacheDirectoryPath(SentryUnityOptions options, IApplication application)
        {
            options.CacheDirectoryPath ??= application.PersistentDataPath;
            Log(options.DiagnosticLogger, "Cache Directory", options.CacheDirectoryPath);
        }

        private static void Log(IDiagnosticLogger? logger, string option, object value)
        {
            logger?.Log(SentryLevel.Debug, "Setting Sentry {0} to: {1}", null, option, value);
        }
    }
}
