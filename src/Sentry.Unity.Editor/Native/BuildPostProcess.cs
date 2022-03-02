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

                if (options?.Validate() is not true)
                {
                    logger.LogWarning("Failed to validate Sentry Options. Native support disabled.");
                    return;
                }

                if (!options.WindowsNativeSupportEnabled)
                {
                    logger.LogDebug("Windows Native support disabled through the options.");
                    return;
                }

                addCrashHandler(pathToProject);
                uploadDebugSymbols(logger, pathToProject);

            }
            catch (Exception e)
            {
                logger.LogError("Failed to add the Sentry native integration to the built application", e);
                throw new BuildFailedException("Sentry Native BuildPostProcess failed");
            }
        }

        private static void addCrashHandler(string pathToProject)
        {
            var crashpadPath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins",
                "Windows", "Sentry", "crashpad_handler.exe"));
            var targetPath = Path.Combine(Path.GetDirectoryName(pathToProject), Path.GetFileName(crashpadPath));
            File.Copy(crashpadPath, targetPath, true);
        }

        private static void uploadDebugSymbols(IDiagnosticLogger logger, string pathToProject)
        {
            var cliOptions = SentryCliOptions.LoadCliOptions();
            if (!cliOptions.Validate(logger))
                return;

            // TODO actual symbol upload
            // SentryCli.CreateSentryProperties(pathToProject, cliOptions);
            // SentryCli.AddExecutableToXcodeProject(pathToProject, logger);

            // sentryXcodeProject.AddBuildPhaseSymbolUpload(logger);
        }
    }
}
