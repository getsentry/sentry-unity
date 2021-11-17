using System;
using System.IO;
using System.Runtime.InteropServices;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal class SentryCli
    {
        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        private string _sentryCliPath;

        public SentryCli(IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            var sentryCliName = GetSentryCliPlatformName(application);
            _sentryCliPath = Path.GetFullPath(
                Path.Combine("Packages", SentryPackageInfo.GetName(), "Editor", "sentry-cli", sentryCliName));

            if (!File.Exists(_sentryCliPath))
            {
                throw new FileNotFoundException($"Could not find sentry-cli at path: {_sentryCliPath}");
            }

            SetExecutePermission(_sentryCliPath);
        }

        internal string GetSentryCliPlatformName(IApplication application) =>
            application.Platform switch
            {
                RuntimePlatform.WindowsEditor => "sentry-cli-Windows-x86_64.exe ",
                RuntimePlatform.OSXEditor => "sentry-cli-Darwin-universal",
                RuntimePlatform.LinuxEditor => "sentry-cli-Linux-x86_64 ",
                _ => throw new InvalidOperationException(
                    $"Cannot get sentry-cli for the current platform: {Application.platform}")
            };

        internal void SetExecutePermission(string filePath)
        {
            // 493 is the integer value for permissions 755
            if (chmod(Path.GetFullPath(filePath), 493) != 0)
            {
                throw new UnauthorizedAccessException($"Failed to set permission to {filePath}");
            }
        }

        internal string GetSentryCliPath() => _sentryCliPath;
    }
}
