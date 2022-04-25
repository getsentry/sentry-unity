using System;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    public static class BuildPostProcess
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToProject)
        {
            if (target is not (BuildTarget.iOS or BuildTarget.StandaloneOSX))
            {
                return;
            }

            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions(BuildPipeline.isBuildingPlayer);
            var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

            try
            {
                var packagePath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "Cocoa"));
                var xcframework = new BuildAsset(Path.Combine(packagePath, "Sentry.xcframework"), Path.Combine(pathToProject, "Frameworks"), logger);
                var bridge = new BuildAsset(Path.Combine(packagePath, "SentryNativeBridge.m"), Path.Combine(pathToProject, "Libraries", SentryPackageInfo.GetName()), logger);

                if (!IsEnabled(options, logger))
                {
                    xcframework.DeleteTarget();
                    bridge.DeleteTarget();
                    return;
                }

                logger.LogDebug("Enabling native support for {0}.", pathToProject);

                using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject, target);
                if (target is BuildTarget.iOS)
                {
                    xcframework.CopyToTarget();
                    sentryXcodeProject.AddSentryFramework(xcframework.RelativePath(pathToProject));
                }
                bridge.CopyToTarget();
                sentryXcodeProject.AddSentryNativeBridge(bridge.RelativePath(pathToProject));
                sentryXcodeProject.AddNativeOptions(options!);
                sentryXcodeProject.AddSentryToMain(options!);

                var sentryCliOptions = SentryCliOptions.LoadCliOptions();
                if (sentryCliOptions.IsValid(logger))
                {
                    SentryCli.CreateSentryProperties(pathToProject, sentryCliOptions);
                    SentryCli.AddExecutableToXcodeProject(pathToProject, logger);
                    sentryXcodeProject.AddBuildPhaseSymbolUpload(logger);
                }
            }
            catch (Exception e)
            {
                logger.LogError("Failed to add the Sentry framework to the generated Xcode project", e);
            }
        }

        private static bool IsEnabled(SentryUnityOptions? options, IDiagnosticLogger logger)
        {
            if (options?.IsValid() is not true)
            {
                logger.LogWarning("Failed to validate Sentry Options. Native support disabled.");
                return false;
            }
            if (!options.IosNativeSupportEnabled)
            {
                logger.LogDebug("Native support disabled through the options.");
                return false;
            }
            return true;
        }
    }

    internal class BuildAsset
    {
        internal readonly string sourcePath;
        internal readonly string targetPath;
        readonly string targetDir;
        readonly IDiagnosticLogger logger;

        internal BuildAsset(string sourcePath, string targetDir, IDiagnosticLogger logger)
        {
            this.sourcePath = sourcePath;
            this.targetDir = targetDir;
            this.targetPath = Path.Combine(targetDir, Path.GetFileName(sourcePath));
            this.logger = logger;
        }

        internal string RelativePath(string basePath)
        {
            if (targetPath.StartsWith(basePath))
            {
                return targetPath.Substring(basePath.Length).TrimStart('/', '\\');
            }
            else
            {
                throw new Exception("Cannot produce a relative path for the given build asset"
                        + $" '{targetPath}' because doesn't start with the base path '{basePath}'.");
            }
        }

        internal bool TargetExists() => Directory.Exists(targetPath) || File.Exists(targetPath);

        internal void DeleteTarget(bool silent = false)
        {
            if (Directory.Exists(targetPath))
            {
                if (!silent)
                {
                    logger.LogDebug("Removing '{0}' recursively", targetPath);
                }
                Directory.Delete(targetPath, true);
            }
            else if (File.Exists(targetPath))
            {
                if (!silent)
                {
                    logger.LogDebug("Removing '{0}'", targetPath);
                }
                File.Delete(targetPath);
            }
        }

        internal void CopyToTarget()
        {
            if (TargetExists())
            {
                logger.LogDebug("Replacing '{0}' with '{1}'", targetPath, sourcePath);
                DeleteTarget(true);
            }
            else
            {
                logger.LogDebug("Copying from: '{0}' to '{1}'", sourcePath, targetPath);
            }

            Directory.CreateDirectory(targetDir);
            FileUtil.CopyFileOrDirectory(sourcePath, targetPath);

            if (!TargetExists())
            {
                throw new IOException($"Failed to copy '{sourcePath}' to '{targetPath}'");
            }
        }
    }
}
