using System;
using System.IO;
using System.Linq;
using Sentry.Extensibility;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace Sentry.Unity.Editor.iOS
{
    internal class SentryXcodeProject : IDisposable
    {
        private const string FrameworkName = "Sentry.framework";
        internal const string SymbolUploadPhaseName = "SymbolUpload";

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
            var relativeFrameworkPath = Path.Combine("Frameworks", FrameworkName);
            var frameworkGuid = _project.AddFile(relativeFrameworkPath, relativeFrameworkPath);

            var mainTargetGuid = _project.GetUnityMainTargetGuid();
            var unityFrameworkTargetGuid = _project.GetUnityFrameworkTargetGuid();

            _project.AddFrameworkToProject(mainTargetGuid, FrameworkName, false);
            _project.AddFrameworkToProject(unityFrameworkTargetGuid, FrameworkName, false);

            _project.AddFileToEmbedFrameworks(mainTargetGuid, frameworkGuid); // Embedding the framework because it's dynamic and needed at runtime

            _project.SetBuildProperty(mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks/");
            _project.SetBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            _project.AddBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks/");

            _project.AddBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks/");
            _project.AddBuildProperty(unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks/");

            _project.SetBuildProperty(mainTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");
            _project.SetBuildProperty(unityFrameworkTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");

            _project.AddBuildProperty(mainTargetGuid, "OTHER_LDFLAGS", "-ObjC");
        }

        public void AddBuildPhaseSymbolUpload(IDiagnosticLogger? logger)
        {
            if (MainTargetContainsSymbolUploadBuildPhase())
            {
                logger?.LogDebug("Build phase '{0}' already added.", SymbolUploadPhaseName);
                return;
            }

            var mainTargetGuid = _project.GetUnityMainTargetGuid();
            _project.AddShellScriptBuildPhase(mainTargetGuid,
                SymbolUploadPhaseName,
                "/bin/sh",
                @"export SENTRY_PROPERTIES=sentry.properties
if [[ ""$BITCODE_GENERATION_MODE"" == ""marker"" ]] ; then
    ERROR=$(./sentry-cli-Darwin-universal upload-dif $BUILT_PRODUCTS_DIR > ./sentry-symbols-upload.log 2>&1 &)
    if [ ! $? -eq 0 ] ; then
        echo ""warning: sentry-cli - $ERROR""
    fi
fi"
            );
        }

        public void AddNativeOptions(SentryUnityOptions options)
        {
            _nativeOptions.CreateFile(Path.Combine(_projectRoot, _optionsPath), options);
            _project.AddFile(_optionsPath, _optionsPath);
        }

        public void AddSentryToMain(SentryUnityOptions options) =>
            _nativeMain.AddSentry(Path.Combine(_projectRoot, _mainPath), options.DiagnosticLogger);

        internal bool MainTargetContainsSymbolUploadBuildPhase()
        {
            var allBuildPhases = _project.GetAllBuildPhasesForTarget(_project.GetUnityMainTargetGuid());
            return allBuildPhases.Any(buildPhase => _project.GetBuildPhaseName(buildPhase) == SymbolUploadPhaseName);
        }

        internal string ProjectToString() => _project.WriteToString();

        public void Dispose() => _project.WriteToFile(_projectPath);
    }
}
