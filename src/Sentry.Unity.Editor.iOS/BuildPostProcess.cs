using System;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Editor.ConfigurationWindow;
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

            var (options, cliOptions) = SentryScriptableObject.ConfiguredBuildTimeOptions();
            var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

            try
            {
                // The Sentry.xcframework ends in '~' to stop Unity from trying to import the .frameworks.
                // Otherwise, Unity tries to export those with the XCode build.
                var frameworkPath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "iOS", "Sentry.xcframework~"));
                CopyFramework(frameworkPath, Path.Combine(pathToProject, "Frameworks", "Sentry.xcframework"), options?.DiagnosticLogger);

                var nativeBridgePath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "iOS", "SentryNativeBridge.m"));
                CopyFile(nativeBridgePath, Path.Combine(pathToProject, "Libraries", SentryPackageInfo.GetName(), "SentryNativeBridge.m"), options?.DiagnosticLogger);

                using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject);
                sentryXcodeProject.AddSentryFramework();
                sentryXcodeProject.AddSentryNativeBridge();

                if (options is null)
                {
                    logger.LogWarning("iOS native support disabled because Sentry has not been configured. " +
                                      "You can do that through the editor: {0}", SentryWindow.EditorMenuPath);
                    return;
                }

                if (!options.IsValid())
                {
                    logger.LogWarning("iOS native support disabled.");
                    return;
                }

                if (!options.IosNativeSupportEnabled)
                {
                    logger.LogDebug("iOS native support disabled through the options.");
                    return;
                }

                sentryXcodeProject.AddNativeOptions(options);
                sentryXcodeProject.AddSentryToMain(options);

                if (cliOptions?.IsValid(logger, EditorUserBuildSettings.development) is true)
                {
                    SentryCli.CreateSentryProperties(pathToProject, cliOptions, options);
                    SentryCli.AddExecutableToXcodeProject(pathToProject, logger);
                    sentryXcodeProject.AddBuildPhaseSymbolUpload(logger, cliOptions);
                }
                else if (options.Il2CppLineNumberSupportEnabled)
                {
                    logger.LogWarning("The IL2CPP line number support requires the debug symbol upload to be enabled.");
                }
            }
            catch (Exception e)
            {
                logger.LogError("Failed to add the Sentry framework to the generated Xcode project", e);
            }
        }

        internal static void CopyFramework(string sourcePath, string targetPath, IDiagnosticLogger? logger)
        {
            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"Failed to find '{sourcePath}'");
            }

            if (Directory.Exists(targetPath))
            {
                logger?.LogDebug("'{0}' has already been copied to '{1}'", Path.GetFileName(targetPath), targetPath);
                return;
            }

            if (Directory.Exists(sourcePath))
            {
                logger?.LogDebug("Copying from: '{0}' to '{1}'", sourcePath, targetPath);

                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(targetPath)));
                FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
            }

            if (!Directory.Exists(targetPath))
            {
                throw new DirectoryNotFoundException($"Failed to copy '{sourcePath}' to '{targetPath}'");
            }
        }

        internal static void CopyFile(string sourcePath, string targetPath, IDiagnosticLogger? logger)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Failed to find '{sourcePath}'");
            }

            if (File.Exists(targetPath))
            {
                logger?.LogDebug("'{0}' has already been copied to '{1}'", Path.GetFileName(targetPath), targetPath);
                return;
            }

            if (File.Exists(sourcePath))
            {
                logger?.LogDebug("Copying from: '{0}' to '{1}'", sourcePath, targetPath);

                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(targetPath)));
                FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
            }

            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException($"Failed to copy '{sourcePath}' to '{targetPath}'");
            }
        }
    }
}
