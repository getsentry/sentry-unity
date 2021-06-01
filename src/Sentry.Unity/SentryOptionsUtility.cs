using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(SentryUnityOptions options, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            FillRelease(options, application);
            FillEnvironment(options, application);
            FillCacheDirectoryPath(options, application);
        }

        private static void FillRelease(SentryUnityOptions options, IApplication application)
        {
            options.Release ??= application.ProductName is string productName
                && !string.IsNullOrWhiteSpace(productName)
                    ? $"{productName}@{application.Version}"
                    : $"{application.Version}";

            Log(options.DiagnosticLogger, "Release", options.Release);
        }

        private static void FillEnvironment(SentryUnityOptions options, IApplication application)
        {
            options.Environment ??= application.IsEditor ? "editor" : "production";
            Log(options.DiagnosticLogger, "Environment", options.Environment);
        }

        private static void FillCacheDirectoryPath(SentryUnityOptions options, IApplication application)
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
