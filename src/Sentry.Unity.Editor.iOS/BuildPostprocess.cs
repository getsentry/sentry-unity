using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace Sentry.Unity.Editor
{
    public static class BuildPostprocess
    {
        private const string Include = "#include <Sentry/Sentry.h>\n";

        private const string Init = @"        [SentrySDK startWithConfigureOptions:^(SentryOptions *options) {
            options.dsn = @""https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417"";
            options.enableAutoSessionTracking = NO;
            options.debug = YES;
        }];

";

        private const string FrameworkLocation = "Frameworks/Plugins/iOS"; // The path where the framework is stored
        private const string FrameworkName = "Sentry.framework";

        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projectPath));

            ModifyMain(pathToBuiltProject);

            var targetGuid = project.GetUnityMainTargetGuid();
            var fileGuid = project.AddFile(
                Path.Combine(FrameworkLocation, FrameworkName),
                Path.Combine(FrameworkLocation, FrameworkName));

            project.AddFileToBuild(targetGuid, fileGuid); // Ensures that the framework shows up on 'Link Binary with Libraries'
            project.AddFileToEmbedFrameworks(targetGuid, fileGuid); // Embedding the framework because it's dynamic and needed at runtime

            project.SetBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            project.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", $"$(PROJECT_DIR)/{FrameworkLocation}/");

            // project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");

            project.WriteToFile(projectPath);
        }

        private static void ModifyMain(string pathToBuiltProject)
        {
            var mainPath = Path.Combine(pathToBuiltProject, "MainApp", "main.mm");
            if (!File.Exists(mainPath))
            {
                return;
            }

            var text = File.ReadAllText(mainPath);

            var includeRegex = new Regex(@"\#include \<Sentry\/Sentry\.h\>");
            if (includeRegex.Match(text).Success)
            {
                return;
            }

            text = Include + text;

            var initRegex = new Regex(@"int main\(int argc, char\* argv\[\]\)\n{\n\s+@autoreleasepool\n.\s+{\n");
            var match = initRegex.Match(text);
            if (match.Success)
            {
                Debug.Log($"great success at {match.Index}");
                text = text.Insert(match.Index + match.Length, Init);
            }

            File.WriteAllText(mainPath, text);
        }
    }
}
