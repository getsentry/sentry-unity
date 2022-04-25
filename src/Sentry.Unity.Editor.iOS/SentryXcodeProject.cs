using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sentry.Extensibility;
using UnityEditor;

// The decision to use reflection here came after many attempts to avoid it. Allowing customers to export an xcode
// project outside a Mac while not requiring users to install the iOS tools on Windows and Linux became a challenge
// More context: https://github.com/getsentry/sentry-unity/issues/400, https://github.com/getsentry/sentry-unity/issues/588
// using UnityEditor.iOS.Xcode;
// using UnityEditor.iOS.Xcode.Extensions;

namespace Sentry.Unity.Editor.iOS
{
    internal class SentryXcodeProject : IDisposable
    {
        internal const string OptionsName = "SentryOptions.m";
        internal const string SymbolUploadPhaseName = "SymbolUpload";

        private readonly string _mainPath = Path.Combine("MainApp", "main.mm");
        private readonly string _optionsPath = Path.Combine("MainApp", OptionsName);
        private readonly string _uploadScript = $@"process_upload_symbols()
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
fi";

        private readonly Type _pbxProjectType = null!;               // Set in constructor or throws
        private readonly Type _pbxProjectExtensionsType = null!;     // Set in constructor or throws
        private readonly object _project = null!;           // Set in constructor or throws

        private readonly string _projectRoot;
        private readonly string _projectPath;

        private string _mainTargetGuid = null!;             // Set when opening the project
        private string _unityFrameworkTargetGuid = null!;   // Set when opening the project

        private readonly INativeMain _nativeMain;
        private readonly INativeOptions _nativeOptions;

        internal SentryXcodeProject(string projectRoot, BuildTarget buildTarget) : this(projectRoot, buildTarget, new NativeMain(), new NativeOptions())
        { }

        internal SentryXcodeProject(
            string projectRoot,
            BuildTarget buildTarget,
            INativeMain mainModifier,
            INativeOptions sentryNativeOptions)
        {
            var xcodeAssembly = Assembly.Load("UnityEditor.iOS.Extensions.Xcode");
            _pbxProjectType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.PBXProject");
            _pbxProjectExtensionsType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.Extensions.PBXProjectExtensions");

            _project = Activator.CreateInstance(_pbxProjectType);

            _projectRoot = projectRoot;
            if (buildTarget is BuildTarget.iOS)
            {
                _projectPath = (string)_pbxProjectType.GetMethod("GetPBXProjectPath", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new[] { _projectRoot });
            }
            else
            {
                _projectPath = Path.Combine(_projectRoot, "GameAssembly.xcodeproj/project.pbxproj");
            }

            _nativeMain = mainModifier;
            _nativeOptions = sentryNativeOptions;
        }

        public static SentryXcodeProject Open(string path, BuildTarget buildTarget)
        {
            var xcodeProject = new SentryXcodeProject(path, buildTarget);
            xcodeProject.ReadFromProjectFile();

            return xcodeProject;
        }

        internal void ReadFromProjectFile()
        {
            if (!File.Exists(_projectPath))
            {
                throw new FileNotFoundException("Could not locate generated Xcode project at", _projectPath);
            }

            _pbxProjectType.GetMethod("ReadFromString", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new[] { File.ReadAllText(_projectPath) });
            _mainTargetGuid = (string)_pbxProjectType.GetMethod("GetUnityMainTargetGuid", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, null);
            _unityFrameworkTargetGuid = (string)_pbxProjectType.GetMethod("GetUnityFrameworkTargetGuid", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, null);
        }

        public void AddSentryFramework(string relativeFrameworkPath)
        {
            var frameworkName = Path.GetFileName(relativeFrameworkPath);
            var frameworkGuid = (string)_pbxProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { relativeFrameworkPath, relativeFrameworkPath, 1 }); // 1 is PBXSourceTree.Source

            var addFrameworkToProjectMethod = _pbxProjectType.GetMethod("AddFrameworkToProject", BindingFlags.Public | BindingFlags.Instance);
            addFrameworkToProjectMethod.Invoke(_project, new object[] { _mainTargetGuid, frameworkName, false });
            addFrameworkToProjectMethod.Invoke(_project, new object[] { _unityFrameworkTargetGuid, frameworkName, false });

            // Embedding the framework because it's dynamic and needed at runtime
            _pbxProjectExtensionsType.GetMethod("AddFileToEmbedFrameworks", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object?[] { _project, _mainTargetGuid, frameworkGuid, null });

            SetSearchPathBuildProperty("$(inherited)");
            SetSearchPathBuildProperty("$(PROJECT_DIR)/Frameworks/");

            var setBuildPropertyMethod = _pbxProjectType.GetMethod("SetBuildProperty", new[] { typeof(string), typeof(string), typeof(string) });
            setBuildPropertyMethod.Invoke(_project, new object[] { _mainTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" });
            setBuildPropertyMethod.Invoke(_project, new object[] { _unityFrameworkTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" });

            _pbxProjectType.GetMethod("AddBuildProperty", new[] { typeof(string), typeof(string), typeof(string) })
                .Invoke(_project, new object[] { _mainTargetGuid, "OTHER_LDFLAGS", "-ObjC" });
        }

        public void AddSentryNativeBridge(string relativeBridgePath)
        {
            var bridgeGuid = (string)_pbxProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { relativeBridgePath, relativeBridgePath, 1 }); // 1 is PBXSourceTree.Source

            _pbxProjectType.GetMethod("AddFileToBuild", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new[] { _unityFrameworkTargetGuid, bridgeGuid });
        }

        // Used for testing
        internal void SetSearchPathBuildProperty(string path)
        {
            _pbxProjectType.GetMethod("AddBuildProperty", new[] { typeof(string[]), typeof(string), typeof(string) })
                .Invoke(_project, new object[] { new[] { _mainTargetGuid, _unityFrameworkTargetGuid }, "FRAMEWORK_SEARCH_PATHS", path });
        }

        public void AddBuildPhaseSymbolUpload(IDiagnosticLogger? logger)
        {
            if (MainTargetContainsSymbolUploadBuildPhase())
            {
                logger?.LogDebug("Build phase '{0}' already added.", SymbolUploadPhaseName);
                return;
            }

            _pbxProjectType.GetMethod("AddShellScriptBuildPhase", new[] { typeof(string), typeof(string), typeof(string), typeof(string) })
                .Invoke(_project, new object[] { _mainTargetGuid, SymbolUploadPhaseName, "/bin/sh", _uploadScript });
        }

        public void AddNativeOptions(SentryUnityOptions options)
        {
            _nativeOptions.CreateFile(Path.Combine(_projectRoot, _optionsPath), options);
            _pbxProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { _optionsPath, _optionsPath, 1 }); // 1 is PBXSourceTree.Source
        }

        public void AddSentryToMain(SentryUnityOptions options) =>
            _nativeMain.AddSentry(Path.Combine(_projectRoot, _mainPath), options.DiagnosticLogger);

        internal bool MainTargetContainsSymbolUploadBuildPhase()
        {
            var allBuildPhases = (string[])_pbxProjectType.GetMethod("GetAllBuildPhasesForTarget", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { _mainTargetGuid });
            var getBuildPhaseNameMethod = _pbxProjectType.GetMethod("GetBuildPhaseName", BindingFlags.Public | BindingFlags.Instance);

            return allBuildPhases.Any(buildPhase =>
            {
                var buildPhaseName = (string)getBuildPhaseNameMethod.Invoke(_project, new[] { buildPhase });
                return buildPhaseName == SymbolUploadPhaseName;
            });
        }

        internal string ProjectToString() =>
            (string)_pbxProjectType.GetMethod("WriteToString", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, null);

        public void Dispose() =>
            _pbxProjectType.GetMethod("WriteToFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new[] { _projectPath });
    }
}
