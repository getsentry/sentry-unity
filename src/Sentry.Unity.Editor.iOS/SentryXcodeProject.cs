using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    internal class SentryXcodeProject
    {
        // TODO: IMPORTANT! This HAS to match the location where unity copies the framework to and matches the location in the project
        private const string PackageName = "io.sentry.unity";
        private const string FrameworkName = "Sentry.framework";

        private const string MainPathRelative = "MainApp/main.mm";
        private const string OptionsPathRelative = "MainApp/SentryOptions.m";

        private readonly string _pathToProject;
        private readonly PBXProject _project;
        private readonly string _projectPath;
        private readonly string _frameworkLocation = ""; // The path within the Xcode project to the framework

        private IMainModifier _mainModifier;
        private ISentryNativeOptions _sentryNativeOptions;

        internal SentryXcodeProject(string pathToProject, IMainModifier? mainModifier, ISentryNativeOptions? sentryNativeOptions)
        {
            _pathToProject = pathToProject;

            _projectPath = PBXProject.GetPBXProjectPath(pathToProject);
            _project = new PBXProject();
            _project.ReadFromString(File.ReadAllText(_projectPath));

            _frameworkLocation = GetFrameworkPath(_pathToProject);

            _mainModifier = mainModifier ?? new MainModifier();
            _sentryNativeOptions = sentryNativeOptions ?? new SentryNativeOptions();
        }

        public static SentryXcodeProject Open(
            string path,
            IMainModifier? mainModifier = null,
            ISentryNativeOptions? sentryNativeOptions = null)
        {
            return new (path, mainModifier, sentryNativeOptions);
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
            AddSentryFramework(_project, _frameworkLocation);
        }

        internal static void AddSentryFramework(PBXProject project, string frameworkLocation)
        {
            var targetGuid = project.GetUnityMainTargetGuid();
            var fileGuid = project.AddFile(
                Path.Combine(frameworkLocation, FrameworkName),
                Path.Combine(frameworkLocation, FrameworkName));

            var unityLinkPhaseGuid = project.GetFrameworksBuildPhaseByTarget(targetGuid);

            project.AddFileToBuildSection(targetGuid, unityLinkPhaseGuid, fileGuid); // Link framework in 'Build Phases > Link Binary with Libraries'
            project.AddFileToEmbedFrameworks(targetGuid, fileGuid); // Embedding the framework because it's dynamic and needed at runtime

            project.SetBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            project.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", $"$(PROJECT_DIR)/{frameworkLocation}/");

            project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
        }

        public void AddNativeOptions(SentryOptions options)
        {
            _sentryNativeOptions.CreateOptionsFile(options, Path.Combine(_pathToProject, OptionsPathRelative));
            _project.AddFile(OptionsPathRelative, OptionsPathRelative);
        }

        public void AddSentryToMain()
        {
            _mainModifier.AddSentry(Path.Combine(_pathToProject, MainPathRelative));
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
