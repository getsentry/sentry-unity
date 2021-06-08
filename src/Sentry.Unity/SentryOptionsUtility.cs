using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    internal static class SentryOptionsUtility
    {
        public static void SetDefaults(SentryUnityOptions options, IApplication? application = null)
        {
            options.TryAttachLogger();

            application ??= ApplicationAdapter.Instance;

            SetRelease(options, application);
            SetEnvironment(options, application);
            SetCacheDirectoryPath(options, application);
        }

        public static void LogOptions(SentryUnityOptions options)
        {
            var logger = options.DiagnosticLogger;
            if (logger == null)
            {
                return;
            }

            LogOption(logger, "Release", options.Release);
            LogOption(logger, "Environment", options.Environment);

            if (options.CacheDirectoryPath == null)
            {
                logger.Log(SentryLevel.Debug, "Offline Caching disabled.");
            }
            else
            {
                LogOption(logger, "Cache Directory", options.CacheDirectoryPath);
            }
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
