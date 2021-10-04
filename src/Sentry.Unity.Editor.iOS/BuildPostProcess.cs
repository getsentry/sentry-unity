using System;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Sentry.Unity.Editor.iOS
{
    public static class BuildPostProcess
    {
        private static string PackageName = "io.sentry.unity";
        private static string PackageNameDev = "io.sentry.unity.dev";

        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions(BuildPipeline.isBuildingPlayer);

            try
            {
                CopyFramework(pathToProject);

                using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject);
                sentryXcodeProject.AddSentryFramework();

                if (options?.Validate() != true)
                {
                    new UnityLogger(new SentryOptions()).LogWarning("Failed to validate Sentry Options. Native support disabled.");
                    return;
                }

                if (!options.IosNativeSupportEnabled)
                {
                    options.DiagnosticLogger?.LogDebug("iOS Native support disabled through the options.");
                    return;
                }

                sentryXcodeProject.AddNativeOptions(options);
                sentryXcodeProject.AddSentryToMain(options);
            }
            catch (Exception e)
            {
                options?.DiagnosticLogger?.LogError("Failed to add the Sentry framework to the generated Xcode project", e);
            }
        }

        private static void CopyFramework(string pathToProject)
        {
            var targetPath = Path.Combine(pathToProject, "Frameworks", "Sentry.framework");
            if (Directory.Exists(targetPath))
            {
                return;
            }

            var packageName = GetPackageName();
            var frameworkDirectory = PlayerSettings.iOS.sdkVersion == iOSSdkVersion.DeviceSDK ? "Device" : "Simulator";

            var frameworkPath = Path.Combine("Packages", packageName, "Plugins", "iOS", frameworkDirectory, "Sentry.framework");
            if (Directory.Exists(frameworkPath))
            {
                Directory.CreateDirectory(Path.Combine(pathToProject, "Frameworks"));
                FileUtil.CopyFileOrDirectoryFollowSymlinks(frameworkPath, targetPath);
            }
            else
            {
                throw new FileNotFoundException($"Failed to copy 'Sentry.framework' from '{frameworkPath}' to Xcode project");
            }
        }

        private static string GetPackageName()
        {
            var packagePath = Path.Combine("Packages", PackageName);
            if (Directory.Exists(Path.Combine(packagePath)))
            {
                return PackageName;
            }

            packagePath = Path.Combine("Packages", PackageNameDev);
            if (Directory.Exists(Path.Combine(packagePath)))
            {
                return PackageNameDev;
            }

            throw new FileNotFoundException("Failed to locate the Sentry package");
        }
    }
}
