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
            if (options is null)
            {
                return;
            }

            var sentryXcodeProject = SentryXcodeProject.Open(pathToProject);

            sentryXcodeProject.AddSentryFramework();
            sentryXcodeProject.CreateNativeOptions(options);
            sentryXcodeProject.AddSentryToMain();

            sentryXcodeProject.Save();
        }
    }
}
