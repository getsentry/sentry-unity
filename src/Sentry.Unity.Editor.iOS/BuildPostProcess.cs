using System;
using Sentry.Extensibility;
using Sentry.Infrastructure;
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
            if (!options.Validate())
            {
                new UnityLogger(new SentryOptions()).LogWarning(
                    "Failed to validate Sentry Options. Xcode project will not be modified.");
                return;
            }

            if (!options!.IosNativeSupportEnabled)
            {
                options.DiagnosticLogger?.LogDebug("iOS Native support disabled. Won't modify the xcode project");
                return;
            }

            try
            {
                using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject, options);
                sentryXcodeProject.AddSentryFramework();
                sentryXcodeProject.AddNativeOptions();
                sentryXcodeProject.AddSentryToMain();
            }
            catch (Exception e)
            {
                options.DiagnosticLogger?.LogError("Failed to add Sentry to the xcode project", e);
            }
        }
    }
}
