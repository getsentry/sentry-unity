using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    public class SentryXcodeProject
    {
        // TODO: IMPORTANT! This HAS to match the location where unity copies the framework to and matches the location in the project
        private const string FrameworkLocation = "Frameworks/Plugins/iOS"; // The path where the framework is stored
        private const string FrameworkName = "Sentry.framework";

        private const string MainPathRelative = "MainApp/main.mm";
        private const string OptionsPathRelative = "MainApp/SentryOptions.m";

        private string _pathToProject;
        private PBXProject _project;
        private string _projectPath;

        public SentryXcodeProject(string pathToProject)
        {
            _pathToProject = pathToProject;

            _projectPath = PBXProject.GetPBXProjectPath(pathToProject);
            _project = new PBXProject();
            _project.ReadFromString(File.ReadAllText(_projectPath));
        }

        public void AddSentryFramework()
        {
            var targetGuid = _project.GetUnityMainTargetGuid();
            var fileGuid = _project.AddFile(
                Path.Combine(FrameworkLocation, FrameworkName),
                Path.Combine(FrameworkLocation, FrameworkName));

            var unityLinkPhaseGuid = _project.GetFrameworksBuildPhaseByTarget(targetGuid);

            _project.AddFileToBuildSection(targetGuid, unityLinkPhaseGuid, fileGuid); // Link framework in 'Build Phases > Link Binary with Libraries'
            _project.AddFileToEmbedFrameworks(targetGuid, fileGuid); // Embedding the framework because it's dynamic and needed at runtime

            _project.SetBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", $"$(PROJECT_DIR)/{FrameworkLocation}/");

            // _project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
            _project.WriteToFile(_projectPath);
        }

        public void CreateNativeOptions(SentryOptions options)
        {
            var nativeOptionsText = SentryNativeOptions.GenerateOptions(options);
            File.WriteAllText(Path.Combine(_pathToProject, OptionsPathRelative), nativeOptionsText);

            _project.AddFile(OptionsPathRelative, OptionsPathRelative);
            _project.WriteToFile(_projectPath);
        }

        public void ModifyMain()
        {
            var mainPath = Path.Combine(_pathToProject, MainPathRelative);
            if (!File.Exists(mainPath))
            {
                Debug.LogError($"Could not find '{mainPath}'.");
                return;
            }

            var modifiedMain = MainModifier.GetModifiedMain(File.ReadAllText(mainPath));
            if (modifiedMain is not null)
            {
                File.WriteAllText(mainPath, modifiedMain);
            }
        }
    }
}
