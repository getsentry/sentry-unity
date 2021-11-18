using System;
using System.IO;
using System.Runtime.InteropServices;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal static class SentryCli
    {
        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        public static void CreateSentryProperties(string path, SentryCliOptions sentryCliOptions)
        {
            // TODO: actually writing that property file.
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
                RuntimePlatform.WindowsEditor => "sentry-cli-Windows-x86_64.exe ",
                RuntimePlatform.OSXEditor => "sentry-cli-Darwin-universal",
                RuntimePlatform.LinuxEditor => "sentry-cli-Linux-x86_64 ",
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

        internal static void SetExecutePermission(string? filePath = null)
        {
            // 493 is the integer value for permissions 755
            if (chmod(Path.GetFullPath(filePath), 493) != 0)
            {
                throw new UnauthorizedAccessException($"Failed to set permission to {filePath}");
            }
        }
    }
}
