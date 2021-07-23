using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class XCodeProject
    {
        private const string Include = "#include <Sentry/Sentry.h>\n#include \"SentryOptions.m\"\n";
        private const string Init = "\t\t[SentrySDK startWithOptions:GetOptions()];\n\n";

        // TODO: IMPORTANT! This HAS to match the location where unity copies the framework to and matches the location in the project
        private const string FrameworkLocation = "Frameworks/Plugins/iOS"; // The path where the framework is stored
        private const string FrameworkName = "Sentry.framework";

        private string _projectPath;
        private PBXProject _project;
        private string? _targetGuid;

        public XCodeProject(string pathToBuiltProject)
        {
            _projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            _project = new PBXProject();
            _project.ReadFromString(File.ReadAllText(_projectPath));

            _targetGuid = _project.GetUnityMainTargetGuid();

            AddSentryFramework();
            ModifyMain();
            AddOptions();
            Save();
        }

        public void AddSentryFramework()
        {
            var fileGuid = _project.AddFile(
                Path.Combine(FrameworkLocation, FrameworkName),
                Path.Combine(FrameworkLocation, FrameworkName));

            var unityLinkPhaseGuid = _project.GetFrameworksBuildPhaseByTarget(_targetGuid);

            _project.AddFileToBuildSection(_targetGuid, unityLinkPhaseGuid, fileGuid); // Link framework in 'Build Phases > Link Binary with Libraries'
            _project.AddFileToEmbedFrameworks(_targetGuid, fileGuid); // Embedding the framework because it's dynamic and needed at runtime

            _project.SetBuildProperty(_targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(_targetGuid, "FRAMEWORK_SEARCH_PATHS", $"$(PROJECT_DIR)/{FrameworkLocation}/");

            // project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
        }

        public void ModifyMain()
        {
            var mainPath = Path.Combine(_projectPath, "MainApp", "main.mm");
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
                text = text.Insert(match.Index + match.Length, Init);
            }

            File.WriteAllText(mainPath, text);
        }

        public void AddOptions()
        {
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options is null)
            {
                return;
            }

            using StreamWriter sw = File.CreateText(Path.Combine(_projectPath, "MainApp", "SentryOptions.m"));

            var templateLines = File.ReadAllLines("Assets/Plugins/Sentry/Template.txt");
            for (var i = 0; i < templateLines.Length; i++)
            {
                Debug.Log($"{templateLines[i]}");

                if (templateLines[i].Contains("dsn"))
                {
                    sw.WriteLine(templateLines[i].Replace("#", options.Dsn));
                    continue;
                }

                if (templateLines[i].Contains("enableAutoSessionTracking"))
                {
                    sw.WriteLine(templateLines[i].Replace("#", "NO"));
                    continue;
                }

                if (templateLines[i].Contains("debug"))
                {
                    sw.WriteLine(templateLines[i].Replace("#", "YES"));
                    continue;
                }

                sw.WriteLine(templateLines[i]);
            }

            var optionsGuid = _project.AddFile(
                Path.Combine("MainApp", "SentryOptions.m"),
                Path.Combine("MainApp", "SentryOptions.m"));
        }

        public void Save()
        {
            _project.WriteToFile(_projectPath);
        }
    }
}
