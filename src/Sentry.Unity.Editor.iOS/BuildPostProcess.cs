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

    internal static void AddSentryToXcodeProject(SentryUnityOptions? options,
        SentryCliOptions? cliOptions,
        IDiagnosticLogger logger,
        string pathToProject)
    {
        if (options is null)
        {
            logger.LogWarning("iOS native support disabled because Sentry has not been configured. " +
                              "You can do that through the editor: {0}", SentryWindow.EditorMenuPath);

            SetupNoOpBridge(logger, pathToProject);
            return;
        }

        if (options.IosNativeSupportEnabled is false)
        {
            logger.LogInfo("iOS native support has been disabled through the options. " +
                           "Native support will not be available at runtime.");

            SetupNoOpBridge(logger, pathToProject);
            return;
        }

        SetupSentry(options, cliOptions, logger, pathToProject);

        // We want to avoid users getting stuck on a cached built output.
        // This can happen if the user appends builds and toggles the `IosNativeInitializationType` from `Standalone` to `Runtime`
        if (options.IosNativeInitializationType is NativeInitializationType.Runtime)
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
        }
    }

    internal static void SetupNoOpBridge(IDiagnosticLogger logger, string pathToProject)
    {
        try
        {
            logger.LogDebug("Copying the NoOp bride to the output project.");

            // The Unity SDK expects the bridge to be there. The Xcode build will break during linking otherwise.
            var nativeBridgePath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "iOS", SentryXcodeProject.NoOpBridgeName));
            CopyFile(nativeBridgePath, Path.Combine(pathToProject, "Libraries", SentryPackageInfo.GetName(), SentryXcodeProject.BridgeName), logger);

            using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject);
            sentryXcodeProject.AddSentryNativeBridge();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add the Sentry NoOp bridge to the output project.");
        }
    }

    internal static void SetupSentry(SentryUnityOptions options,
        SentryCliOptions? cliOptions,
        IDiagnosticLogger logger,
        string pathToProject)
    {
        logger.LogInfo("Attempting to add Sentry to the Xcode project.");

        try
        {
            // The Sentry.xcframework ends in '~' to hide it from Unity. This prevents Unity from exporting it with the XCode build.
            // Ideally, we would let Unity copy this over but:
            // - Detection of `.xcframework` as datatype and non-folder happened in Unity 2021
            // - Without a `.meta` file we cannot opt-in embedding the framework
            // - Even if Unity copies it, the framework still requires to be 'linked with binary' for it to work
            var frameworkPath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "iOS", SentryXcodeProject.FrameworkName + "~"));
            CopyFramework(frameworkPath, Path.Combine(pathToProject, "Frameworks", SentryXcodeProject.FrameworkName), logger);

            var nativeBridgePath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "iOS", SentryXcodeProject.BridgeName));
            CopyFile(nativeBridgePath, Path.Combine(pathToProject, "Libraries", SentryPackageInfo.GetName(), SentryXcodeProject.BridgeName), logger);

            using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject, logger);
            sentryXcodeProject.AddSentryFramework();
            sentryXcodeProject.AddSentryNativeBridge();

            if (options.IosNativeInitializationType is NativeInitializationType.BuildTime)
            {
                sentryXcodeProject.AddNativeOptions(options, NativeOptions.CreateFile);
                sentryXcodeProject.AddSentryToMain(options);
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
