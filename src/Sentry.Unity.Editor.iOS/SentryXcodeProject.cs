using System;
using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    internal class SentryXcodeProject : IDisposable
    {
        private const string FrameworkName = "Sentry.framework";

        private const string MainPath = "MainApp/main.mm";
        private const string OptionsPath = "MainApp/SentryOptions.m";
        private const string UnityPackageFrameworkRoot = "Frameworks/io.sentry.unity";

        private readonly string _projectRoot;
        private readonly SentryUnityOptions _options;

        private readonly PBXProject _project;
        private readonly string _projectPath;

        internal string? RelativeFrameworkPath { get; set; }

        private readonly INativeMain _nativeMain;
        private readonly INativeOptions _nativeOptions;

        public SentryXcodeProject(
            string projectRoot,
            SentryUnityOptions options)
            : this(projectRoot, options, new NativeMain(), new NativeOptions())
        {
        }

        internal SentryXcodeProject(
            string projectRoot,
            SentryUnityOptions options,
            INativeMain mainModifier,
            INativeOptions sentryNativeOptions)
        {
            _projectRoot = projectRoot;
            _options = options;

            _projectPath = PBXProject.GetPBXProjectPath(projectRoot);
            _project = new PBXProject();

            _nativeMain = mainModifier;
            _nativeOptions = sentryNativeOptions;
        }

        public static SentryXcodeProject Open(string path, SentryUnityOptions options)
        {
            var xcodeProject = new SentryXcodeProject(path, options);
            xcodeProject.ReadFromProjectFile();
            xcodeProject.SetRelativeFrameworkPath();

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

        internal void SetRelativeFrameworkPath()
        {
            if (Directory.Exists(Path.Combine(_projectRoot, UnityPackageFrameworkRoot)))
            {
                RelativeFrameworkPath = Path.Combine(UnityPackageFrameworkRoot, "Plugins", "iOS");
                return;
            }

            // For dev purposes - The framework path contains the package name
            var relativeFrameworkPath = UnityPackageFrameworkRoot + ".dev";
            if (Directory.Exists(Path.Combine(_projectRoot, relativeFrameworkPath)))
            {
                RelativeFrameworkPath = Path.Combine(relativeFrameworkPath, "Plugins", "iOS");
                return;
            }

            throw new FileNotFoundException("Could not locate the Sentry package inside the 'Frameworks' directory");
        }

        public void AddSentryFramework()
        {
            var targetGuid = _project.GetUnityMainTargetGuid();
            var frameworkPath = Path.Combine(RelativeFrameworkPath, FrameworkName);
            var frameworkGuid = _project.AddFile(frameworkPath, frameworkPath);

            var unityLinkPhaseGuid = _project.GetFrameworksBuildPhaseByTarget(targetGuid);

            _project.AddFileToBuildSection(targetGuid, unityLinkPhaseGuid,
                frameworkGuid); // Link framework in 'Build Phases > Link Binary with Libraries'
            _project.AddFileToEmbedFrameworks(targetGuid,
                frameworkGuid); // Embedding the framework because it's dynamic and needed at runtime

            _project.SetBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", $"$(PROJECT_DIR)/{RelativeFrameworkPath}/");

            _project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
        }

        public void AddNativeOptions()
        {
            _nativeOptions.CreateFile(Path.Combine(_projectRoot, OptionsPath), _options);
            _project.AddFile(OptionsPath, OptionsPath);
        }

        public void AddSentryToMain() =>
            _nativeMain.AddSentry(Path.Combine(_projectRoot, MainPath), _options.DiagnosticLogger);

        internal string ProjectToString() => _project.WriteToString();

        public void Dispose() => _project.WriteToFile(_projectPath);
    }
}
