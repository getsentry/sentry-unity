using System;
using System.IO;
using System.Linq;
using Sentry.Extensibility;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

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

        private string _mainTargetGuid = null!;           // Gets set when opening the project
        private string _unityFrameworkTargetGuid = null!; // Gets set when opening the project

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
            _mainTargetGuid = _project.GetUnityMainTargetGuid();
            _unityFrameworkTargetGuid = _project.GetUnityFrameworkTargetGuid();
        }

        public void AddSentryFramework()
        {
            var relativeFrameworkPath = Path.Combine("Frameworks", FrameworkName);
            var frameworkGuid = _project.AddFile(relativeFrameworkPath, relativeFrameworkPath);

            _project.AddFrameworkToProject(_mainTargetGuid, FrameworkName, false);
            _project.AddFrameworkToProject(_unityFrameworkTargetGuid, FrameworkName, false);

            _project.AddFileToEmbedFrameworks(_mainTargetGuid, frameworkGuid); // Embedding the framework because it's dynamic and needed at runtime

            SetSearchPathBuildProperty("$(inherited)");
            SetSearchPathBuildProperty("$(PROJECT_DIR)/Frameworks/");

            _project.SetBuildProperty(_mainTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");
            _project.SetBuildProperty(_unityFrameworkTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");

            _project.AddBuildProperty(_mainTargetGuid, "OTHER_LDFLAGS", "-ObjC");
        }

        internal void SetSearchPathBuildProperty(string path)
        {
            _project.AddBuildProperty(_mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", path);
            _project.AddBuildProperty(_unityFrameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", path);
        }

        public void AddBuildPhaseSymbolUpload(IDiagnosticLogger? logger)
        {
            if (MainTargetContainsSymbolUploadBuildPhase())
            {
                logger?.LogDebug("Build phase '{0}' already added.", SymbolUploadPhaseName);
                return;
            }

            _project.AddShellScriptBuildPhase(_mainTargetGuid,
                SymbolUploadPhaseName,
                "/bin/sh",
                $@"process_upload_symbols()
{{
    ERROR=$(./{SentryCli.SentryCliMacOS} upload-dif $BUILT_PRODUCTS_DIR > ./sentry-symbols-upload.log 2>&1 &)
    if [ ! $? -eq 0 ] ; then
        echo ""warning: sentry-cli - $ERROR""
    fi
}}

export SENTRY_PROPERTIES=sentry.properties
if [ ""$ENABLE_BITCODE"" = ""NO"" ] ; then
    echo ""Bitcode is disabled""
    echo ""Uploading symbols""
    process_upload_symbols
else
    echo ""Bitcode is enabled""
    if [ ""$ACTION"" = ""install"" ] ; then
        echo ""Uploading symbols and bcsymbolmaps""
        process_upload_symbols
    else
        echo ""Skipping symbol upload on bitcode enabled and non-install builds""
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
