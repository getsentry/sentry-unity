using System;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;

namespace Sentry.Unity.Editor.Native
{
    public static class BuildPostProcess
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToProject)
        {
            if (target is not (BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64))
            {
                return;
            }

            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions(BuildPipeline.isBuildingPlayer);
            var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

            try
            {
                if (PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) != ScriptingImplementation.IL2CPP)
                {
                    logger.LogWarning("Failed to enable Native support - only availabile with IL2CPP scripting backend.");
                    return;
                }

                if (options?.Validate() != true)
                {
                    logger.LogWarning("Failed to validate Sentry Options. Native support disabled.");
                    return;
                }

                if (!options.WindowsNativeSupportEnabled)
                {
                    logger.LogDebug("Windows Native support disabled through the options.");
                    return;
                }

                var crashpadPath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins",
                    "Windows", "Sentry", "crashpad_handler.exe"));
                var targetPath = Path.Combine(Path.GetDirectoryName(pathToProject)!, Path.GetFileName(crashpadPath));
                File.Copy(crashpadPath, targetPath, true);

                // TODO symbol upload
                // var sentryCliOptions = SentryCliOptions.LoadCliOptions();
                // if (!sentryCliOptions.UploadSymbols)
                // {
                //     logger.LogDebug("Automated symbols upload has been disabled.");
                //     return;
                // }
                //
                // if (EditorUserBuildSettings.development && !sentryCliOptions.UploadDevelopmentSymbols)`
                // {
                //     logger.LogDebug("Automated symbols upload for development builds has been disabled.");
                //     return;
                // }
                //
                // if (!sentryCliOptions.Validate(logger))
                // {
                //     logger.LogWarning("sentry-cli validation failed. Symbols will not be uploaded." +
                //                        "\nYou can disable this warning by disabling the automated symbols upload under " +
                //                        "Tools -> Sentry -> Editor");
                //     return;
                // }
                //
                // SentryCli.CreateSentryProperties(pathToProject, sentryCliOptions);
                // SentryCli.AddExecutableToXcodeProject(pathToProject, logger);
                //
                // sentryXcodeProject.AddBuildPhaseSymbolUpload(logger);
            }
            catch (Exception e)
            {
                logger.LogError("Failed to add the Sentry crash handler to the built application", e);
                throw new BuildFailedException("Sentry Native BuildPostProcess failed");
            }
        }
    }
}
