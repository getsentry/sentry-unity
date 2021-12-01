using System;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Sentry.Unity.Editor.iOS
{
    public static class BuildPostProcess
    {
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
                CopyFrameworkToBuildDirectory(pathToProject, options?.DiagnosticLogger);

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

        private static void CopyFrameworkToBuildDirectory(string pathToProject, IDiagnosticLogger? logger)
        {
            var targetPath = Path.Combine(pathToProject, "Frameworks", "Sentry.framework");
            if (Directory.Exists(targetPath))
            {
                // If the target path already exists we can bail. Unity doesn't allow an appending builds when switching
                // iOS SDK versions and this will make sure we always copy the correct version of the Sentry.framework
                logger?.LogDebug("'Sentry.framework' has already copied to '{0}'", targetPath);
                return;
            }

            var packageName = SentryPackageInfo.GetName();
            var frameworkDirectory = PlayerSettings.iOS.sdkVersion == iOSSdkVersion.DeviceSDK ? "Device" : "Simulator";

            var frameworkPath = Path.GetFullPath(Path.Combine("Packages", packageName, "Plugins", "iOS", frameworkDirectory, "Sentry.framework"));
            if (Directory.Exists(frameworkPath))
            {
                logger?.LogDebug("Copying Sentry.framework from '{0}' to '{1}'", frameworkPath, targetPath);

                Directory.CreateDirectory(Path.Combine(pathToProject, "Frameworks"));
                FileUtil.CopyFileOrDirectoryFollowSymlinks(frameworkPath, targetPath);
            }
            else
            {
                throw new FileNotFoundException($"Failed to copy 'Sentry.framework' from '{frameworkPath}' to Xcode project");
            }
        }
    }
}
