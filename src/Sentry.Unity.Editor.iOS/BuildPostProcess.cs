using System;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Editor.ConfigurationWindow;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;

namespace Sentry.Unity.Editor.iOS;

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

        AddSentryToXcodeProject(options, cliOptions, logger, pathToProject);
    }

    internal static bool IsNativeSupportEnabled(SentryUnityOptions options, IDiagnosticLogger logger)
    {
        if (!options.IsValid())
        {
            logger.LogWarning("Sentry SDK has been disabled. There will be no iOS native support.");
            return false;
        }

        if (!options.IosNativeSupportEnabled)
        {
            logger.LogInfo("The iOS native support has been disabled through the options.");
            return false;
        }

        return true;
    }

    internal static void AddSentryToXcodeProject(SentryUnityOptions? options,
        SentryCliOptions? cliOptions,
        IDiagnosticLogger logger,
        string pathToProject)
    {
        if (options is null)
        {
            logger.LogWarning("iOS native support disabled because Sentry has not been configured. " +
                              "You can do that through the editor: {0}", SentryWindow.EditorMenuPath);
            return;
        }

        if (!IsNativeSupportEnabled(options, logger))
        {
            var mainPath = Path.Combine(pathToProject, SentryXcodeProject.MainPath);
            if (File.Exists(mainPath))
            {
                var main = File.ReadAllText(mainPath);
                if (NativeMain.ContainsSentry(main, logger))
                {
                    throw new BuildFailedException(
                        "The iOS native support has been disabled but the exported project has been modified " +
                        "during a previous build. Select 'Replace' when exporting the project to create a clean project.");
                }
            }

            return;
        }

        SetupSentry(options, cliOptions, logger, pathToProject);
    }

    internal static void SetupSentry(SentryUnityOptions options,
        SentryCliOptions? cliOptions,
        IDiagnosticLogger logger,
        string pathToProject)
    {
        logger.LogInfo("Attempting to setup Sentry in the Xcode project.");

        try
        {
            using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject, logger);

            if (options.IosInitializeNativeFirst)
            {
                logger.LogInfo("Setting up the Cocoa SDK to initialize before the game starts.");

                // We have to add the Sentry Framework manually to the Main Build. Unity does not do that for us.
                // The SDK is not available to the native-options and the init call in `main.mm` otherwise.
                sentryXcodeProject.AddSentryFramework();

                sentryXcodeProject.AddNativeOptions(options, NativeOptions.CreateFile);
                sentryXcodeProject.AddSentryToMain(options);

                logger.LogInfo("Setup of Cocoa SDK to auto-initialize before the game starts finished.");
            }

            if (cliOptions != null && cliOptions.IsValid(logger, EditorUserBuildSettings.development))
            {
                logger.LogInfo("Automatic symbol upload enabled. Adding script to build phase.");

                SentryCli.CreateSentryProperties(pathToProject, cliOptions, options);
                SentryCli.SetupSentryCli(pathToProject, RuntimePlatform.OSXEditor);
                sentryXcodeProject.AddBuildPhaseSymbolUpload(cliOptions);
            }
            else if (options.Il2CppLineNumberSupportEnabled)
            {
                logger.LogWarning("The IL2CPP line number support requires the debug symbol upload to be enabled.");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add the Sentry framework to the generated Xcode project");
            return;
        }

        logger.LogInfo("Successfully added Sentry to the Xcode project.");
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
