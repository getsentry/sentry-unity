using System;
using System.IO;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    internal class SentryXcodeProject : IDisposable
    {
        private const string FrameworkName = "Sentry.framework";

        private readonly string _mainPath = Path.Combine("MainApp", "main.mm");
        private readonly string _optionsPath = Path.Combine("MainApp", "SentryOptions.m");

        private readonly string _projectRoot;
        private readonly PBXProject _project;
        private readonly string _projectPath;

        private readonly INativeMain _nativeMain;
        private readonly INativeOptions _nativeOptions;

        public SentryXcodeProject(string projectRoot) : this(projectRoot, new NativeMain(), new NativeOptions())
        { }

        internal SentryXcodeProject(
            string projectRoot,
            INativeMain mainModifier,
            INativeOptions sentryNativeOptions)
        {
            _projectRoot = projectRoot;
            _projectPath = PBXProject.GetPBXProjectPath(projectRoot);
            _project = new PBXProject();

            _nativeMain = mainModifier;
            _nativeOptions = sentryNativeOptions;
        }

        public static SentryXcodeProject Open(string path)
        {
            var xcodeProject = new SentryXcodeProject(path);
            xcodeProject.ReadFromProjectFile();

            return xcodeProject;
        }

        internal void ReadFromProjectFile()
        {
            if (!File.Exists(_projectPath))
            {
                throw new FileNotFoundException("Could not locate generated Xcode project at", _projectPath);
            }

            _project.ReadFromString(File.ReadAllText(_projectPath));
        }

        public void AddSentryFramework()
        {
            var frameworkPath = Path.Combine(_projectRoot, "Frameworks", FrameworkName);
            var frameworkGuid = _project.AddFile(frameworkPath, frameworkPath);

            var mainTargetGuid = _project.GetUnityMainTargetGuid();
            var unityFrameworkTargetGuid = _project.GetUnityFrameworkTargetGuid();

            _project.AddFrameworkToProject(mainTargetGuid, FrameworkName, false);
            _project.AddFrameworkToProject(unityFrameworkTargetGuid, FrameworkName, false);

            _project.AddFileToEmbedFrameworks(mainTargetGuid, frameworkGuid); // Embedding the framework because it's dynamic and needed at runtime

            _project.SetBuildProperty(mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks/");
            _project.SetBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks/");

            _project.AddBuildProperty(mainTargetGuid, "OTHER_LDFLAGS", "-ObjC");
        }

        public void AddNativeOptions(SentryUnityOptions options)
        {
            _nativeOptions.CreateFile(Path.Combine(_projectRoot, _optionsPath), options);
            _project.AddFile(_optionsPath, _optionsPath);
        }

        public void AddSentryToMain(SentryUnityOptions options) =>
            _nativeMain.AddSentry(Path.Combine(_projectRoot, _mainPath), options.DiagnosticLogger);

        internal string ProjectToString() => _project.WriteToString();

        public void Dispose() => _project.WriteToFile(_projectPath);
    }
}
