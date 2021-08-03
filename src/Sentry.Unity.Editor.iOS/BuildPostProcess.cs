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

            // TODO: check for other criteria why we would stop touching the Xcode project
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options is null || !options.IOSNativeSupportEnabled)
            {
                return;
            }

            var sentryXcodeProject = SentryXcodeProject.Open(pathToProject);
            if (!sentryXcodeProject.ValidateFramework())
            {
                return;
            }

            sentryXcodeProject.AddSentryFramework();
            sentryXcodeProject.AddNativeOptions(options);
            sentryXcodeProject.AddSentryToMain();

            sentryXcodeProject.Save();
        }
    }
}
