using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    internal class SentryXcodeProject
    {
        private const string FrameworkName = "Sentry.framework";

        private const string MainPathRelative = "MainApp/main.mm";
        private const string OptionsPathRelative = "MainApp/SentryOptions.m";

        private readonly string _pathToProject;
        private readonly PBXProject _project;
        private readonly string _projectPath;
        private readonly string _frameworkLocation = ""; // The path within the Xcode project to the framework

        private readonly INativeMain _nativeMain;
        private readonly ISentryNativeOptions _sentryNativeOptions;

        internal SentryXcodeProject(string pathToProject, INativeMain? mainModifier, ISentryNativeOptions? sentryNativeOptions)
        {
            _pathToProject = pathToProject;

            _projectPath = PBXProject.GetPBXProjectPath(pathToProject);
            _project = new PBXProject();
            _project.ReadFromString(File.ReadAllText(_projectPath));

            _frameworkLocation = GetFrameworkPath(_pathToProject);

            _nativeMain = mainModifier ?? new NativeMain();
            _sentryNativeOptions = sentryNativeOptions ?? new NativeOptions();
        }

        public static SentryXcodeProject Open(string path)
        {
            return new (path, null, null);
        }

        internal static string GetFrameworkPath(string pathToProject)
        {
            var relativeFrameworkPath = "Frameworks/io.sentry.unity";
            if (Directory.Exists(Path.Combine(pathToProject, relativeFrameworkPath)))
            {
                return Path.Combine(relativeFrameworkPath, "Plugins", "iOS");
            }

            // For dev purposes - The framework path contains the package name
            relativeFrameworkPath += ".dev";
            if (Directory.Exists(Path.Combine(pathToProject, relativeFrameworkPath)))
            {
                return Path.Combine(relativeFrameworkPath, "Plugins", "iOS");
            }

            Debug.LogWarning("Could not find Sentry in 'Frameworks'.");
            return string.Empty;
        }

        public void AddSentryFramework()
        {
            var targetGuid = _project.GetUnityMainTargetGuid();
            var fileGuid = _project.AddFile(
                Path.Combine(_frameworkLocation, FrameworkName),
                Path.Combine(_frameworkLocation, FrameworkName));

            var unityLinkPhaseGuid = _project.GetFrameworksBuildPhaseByTarget(targetGuid);

            _project.AddFileToBuildSection(targetGuid, unityLinkPhaseGuid, fileGuid); // Link framework in 'Build Phases > Link Binary with Libraries'
            _project.AddFileToEmbedFrameworks(targetGuid, fileGuid); // Embedding the framework because it's dynamic and needed at runtime

            _project.SetBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", $"$(PROJECT_DIR)/{_frameworkLocation}/");

            _project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
        }

        public void AddNativeOptions(SentryOptions options)
        {
            _sentryNativeOptions.CreateFile(options, Path.Combine(_pathToProject, OptionsPathRelative));
            _project.AddFile(OptionsPathRelative, OptionsPathRelative);
        }

        public void AddSentryToMain()
        {
            _nativeMain.AddSentry(Path.Combine(_pathToProject, MainPathRelative));
        }

        public void Save()
        {
            _project.WriteToFile(_projectPath);
        }

        internal string ProjectToString()
        {
            return _project.WriteToString();
        }
    }
}
