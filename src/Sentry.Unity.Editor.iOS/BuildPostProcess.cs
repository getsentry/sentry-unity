using System;
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

            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (!options.Validate())
            {
                return;
            }

            if (!options!.IOSNativeSupportEnabled)
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
