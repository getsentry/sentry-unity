using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

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

            var sentryXcodeProject = new SentryXcodeProject(pathToProject);
            sentryXcodeProject.AddSentryFramework();
            sentryXcodeProject.CreateNativeOptions(options);
            sentryXcodeProject.ModifyMain();
        }
    }
}
