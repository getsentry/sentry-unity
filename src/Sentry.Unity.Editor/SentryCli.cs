using System;
using System.IO;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal static class SentryCli
    {
        internal const string SentryCliWindows = "sentry-cli-Windows-x86_64.exe";
        internal const string SentryCliMacOS = "sentry-cli-Darwin-universal";
        internal const string SentryCliLinux = "sentry-cli-Linux-x86_64";

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        public static string CreateSentryProperties(string propertiesPath, SentryCliOptions sentryCliOptions)
        {
            var propertiesFile = Path.Combine(propertiesPath, "sentry.properties");
            using var properties = File.CreateText(propertiesFile);
            properties.WriteLine($"defaults.org={sentryCliOptions.Organization}");
            properties.WriteLine($"defaults.project={sentryCliOptions.Project}");
            properties.WriteLine($"auth.token={sentryCliOptions.Auth}");
            return propertiesFile;
        }

        public static string SetupSentryCli()
        {
            var sentryCliPlatformName = GetSentryCliPlatformName();
            var sentryCliPath = GetSentryCliPath(sentryCliPlatformName);
            SetExecutePermission(sentryCliPath);

            return sentryCliPath;
        }

        internal static string GetSentryCliPlatformName(IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            return application.Platform switch
            {
                RuntimePlatform.WindowsEditor => SentryCliWindows,
                RuntimePlatform.OSXEditor => SentryCliMacOS,
                RuntimePlatform.LinuxEditor => SentryCliLinux,
                _ => throw new InvalidOperationException(
                    $"Cannot get sentry-cli for the current platform: {Application.platform}")
            };
        }

        internal static string GetSentryCliPath(string sentryCliPlatformName)
        {
            var sentryCliPath = Path.GetFullPath(
                Path.Combine("Packages", SentryPackageInfo.GetName(), "Editor", "sentry-cli", sentryCliPlatformName));

            if (!File.Exists(sentryCliPath))
            {
                throw new FileNotFoundException($"Could not find sentry-cli at path: {sentryCliPath}");
            }

            return sentryCliPath;
        }

        internal static void SetExecutePermission(string? filePath = null, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;
            if (application.Platform == RuntimePlatform.WindowsEditor)
            {
                return;
            }

            // 493 is the integer value for permissions 755
            if (chmod(Path.GetFullPath(filePath), 493) != 0)
            {
                throw new UnauthorizedAccessException($"Failed to set permission to {filePath}");
            }
        }

        internal static void AddExecutableToXcodeProject(string projectPath, IDiagnosticLogger? logger)
        {
            var executableSource = GetSentryCliPath(SentryCliMacOS);
            var executableDestination = Path.Combine(projectPath, SentryCliMacOS);

            if (!Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException($"Xcode project directory not found at {executableDestination}");
            }

            if (File.Exists(executableDestination))
            {
                logger?.LogDebug("sentry-cli executable already found at {0}", executableDestination);
                return;
            }

            File.Copy(executableSource, executableDestination);
            SetExecutePermission(executableDestination);
        }
    }
}
