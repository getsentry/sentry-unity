using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    internal static class SentryDefaultOptionSetter
    {
        public static void SetRelease(SentryUnityOptions options, IApplication? appDomain = null)
        {
            var application = appDomain ?? ApplicationAdapter.Instance;
            options.Release ??= application.ProductName is string productName
                && !string.IsNullOrWhiteSpace(productName)
                    ? $"{productName}@{application.Version}"
                    : $"{application.Version}";

            Log(options.DiagnosticLogger, "Release", options.Release);
        }

        public static void SetEnvironment(SentryUnityOptions options, IApplication? appDomain = null)
        {
            var application = appDomain ?? ApplicationAdapter.Instance;
            options.Environment ??= application.IsEditor ? "editor" : "production";

            Log(options.DiagnosticLogger, "Environment", options.Environment);
        }

        public static void SetCacheDirectoryPath(SentryUnityOptions options, IApplication? appDomain = null)
        {
            var application = appDomain ?? ApplicationAdapter.Instance;

            options.CacheDirectoryPath ??= application.PersistentDataPath;

            Log(options.DiagnosticLogger, "Cache Directory", options.CacheDirectoryPath);
        }

        private static void Log(IDiagnosticLogger? logger, string sentryValue, object value)
        {
            logger?.Log(SentryLevel.Debug, "Setting Sentry " + sentryValue + " to: {0}", null, value);
        }
    }
}
