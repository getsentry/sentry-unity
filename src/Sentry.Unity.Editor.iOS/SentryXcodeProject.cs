using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sentry.Extensibility;

// The decision to use reflection here came after many attempts to avoid it. Allowing customers to export an xcode
// project outside a Mac while not requiring users to install the iOS tools on Windows and Linux became a challenge
// More context: https://github.com/getsentry/sentry-unity/issues/400, https://github.com/getsentry/sentry-unity/issues/588
// using UnityEditor.iOS.Xcode;
// using UnityEditor.iOS.Xcode.Extensions;

namespace Sentry.Unity.Editor.iOS
{
    internal class SentryXcodeProject : IDisposable
    {
        private static readonly Type PBXProjectType = null!; // Expected to be set in constructor or throws
        private static readonly Type PBXProjectExtensionsType = null!; // Expected to be set in constructor or throws

        internal const string FrameworkName = "Sentry.framework";
        internal const string BridgeName = "SentryNativeBridge.m";
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

        private readonly string _projectRoot;
        private readonly object _project = null!; // Expected to be set in constructor or throws
        private readonly string _projectPath;

        private string _mainTargetGuid = null!;           // Gets set when opening the project
        private string _unityFrameworkTargetGuid = null!; // Gets set when opening the project

        private readonly INativeMain _nativeMain;
        private readonly INativeOptions _nativeOptions;

        static SentryXcodeProject()
        {
            var xcodeAssembly = Assembly.Load("UnityEditor.iOS.Extensions.Xcode");
            PBXProjectType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.PBXProject");
            PBXProjectExtensionsType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.Extensions.PBXProjectExtensions");
        }

        internal SentryXcodeProject(string projectRoot) : this(projectRoot, new NativeMain(), new NativeOptions())
        { }

        internal SentryXcodeProject(
            string projectRoot,
            INativeMain mainModifier,
            INativeOptions sentryNativeOptions)
        {
            _projectRoot = projectRoot;
            _projectPath = (string)PBXProjectType.GetMethod("GetPBXProjectPath", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new[] { projectRoot });

            _project = Activator.CreateInstance(PBXProjectType);

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

            PBXProjectType.GetMethod("ReadFromString", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new[] { File.ReadAllText(_projectPath) });
            _mainTargetGuid = (string)PBXProjectType.GetMethod("GetUnityMainTargetGuid", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, null);
            _unityFrameworkTargetGuid = (string)PBXProjectType.GetMethod("GetUnityFrameworkTargetGuid", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, null);
        }

        public void AddSentryFramework()
        {
            var relativeFrameworkPath = Path.Combine("Frameworks", FrameworkName);
            var frameworkGuid = (string)PBXProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { relativeFrameworkPath, relativeFrameworkPath, 1 }); // 1 is PBXSourceTree.Source

            var addFrameworkToProjectMethod = PBXProjectType.GetMethod("AddFrameworkToProject", BindingFlags.Public | BindingFlags.Instance);
            addFrameworkToProjectMethod.Invoke(_project, new object[] { _mainTargetGuid, FrameworkName, false });
            addFrameworkToProjectMethod.Invoke(_project, new object[] { _unityFrameworkTargetGuid, FrameworkName, false });

            // Embedding the framework because it's dynamic and needed at runtime
            PBXProjectExtensionsType.GetMethod("AddFileToEmbedFrameworks", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object?[] { _project, _mainTargetGuid, frameworkGuid, null });

            SetSearchPathBuildProperty("$(inherited)");
            SetSearchPathBuildProperty("$(PROJECT_DIR)/Frameworks/");

            var setBuildPropertyMethod = PBXProjectType.GetMethod("SetBuildProperty", new[] { typeof(string), typeof(string), typeof(string) });
            setBuildPropertyMethod.Invoke(_project, new object[] { _mainTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" });
            setBuildPropertyMethod.Invoke(_project, new object[] { _unityFrameworkTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" });

            PBXProjectType.GetMethod("AddBuildProperty", new[] { typeof(string), typeof(string), typeof(string) })
                .Invoke(_project, new object[] { _mainTargetGuid, "OTHER_LDFLAGS", "-ObjC" });
        }

        public void AddSentryNativeBridge()
        {
            var relativeBridgePath = Path.Combine("Libraries", SentryPackageInfo.GetName(), BridgeName);
            var bridgeGuid = (string)PBXProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { relativeBridgePath, relativeBridgePath, 1 }); // 1 is PBXSourceTree.Source

            PBXProjectType.GetMethod("AddFileToBuild", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new[] { _unityFrameworkTargetGuid, bridgeGuid });
        }

        // Used for testing
        internal void SetSearchPathBuildProperty(string path)
        {
            PBXProjectType.GetMethod("AddBuildProperty", new[] { typeof(string[]), typeof(string), typeof(string) })
                .Invoke(_project, new object[] { new[] { _mainTargetGuid, _unityFrameworkTargetGuid }, "FRAMEWORK_SEARCH_PATHS", path });
        }

        public void AddBuildPhaseSymbolUpload(IDiagnosticLogger? logger)
        {
            if (MainTargetContainsSymbolUploadBuildPhase())
            {
                logger?.LogDebug("Build phase '{0}' already added.", SymbolUploadPhaseName);
                return;
            }

            PBXProjectType.GetMethod("AddShellScriptBuildPhase", new[] { typeof(string), typeof(string), typeof(string), typeof(string) })
                .Invoke(_project, new object[] { _mainTargetGuid, SymbolUploadPhaseName, "/bin/sh", _uploadScript });
        }

        public void AddNativeOptions(SentryUnityOptions options)
        {
            _nativeOptions.CreateFile(Path.Combine(_projectRoot, _optionsPath), options);
            PBXProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { _optionsPath, _optionsPath, 1 }); // 1 is PBXSourceTree.Source
        }

        public void AddSentryToMain(SentryUnityOptions options) =>
            _nativeMain.AddSentry(Path.Combine(_projectRoot, _mainPath), options.DiagnosticLogger);

        internal bool MainTargetContainsSymbolUploadBuildPhase()
        {
            var allBuildPhases = (string[])PBXProjectType.GetMethod("GetAllBuildPhasesForTarget", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new object[] { _mainTargetGuid });
            var getBuildPhaseNameMethod = PBXProjectType.GetMethod("GetBuildPhaseName", BindingFlags.Public | BindingFlags.Instance);

            return allBuildPhases.Any(buildPhase =>
            {
                var buildPhaseName = (string)getBuildPhaseNameMethod.Invoke(_project, new[] { buildPhase });
                return buildPhaseName == SymbolUploadPhaseName;
            });
        }

        internal string ProjectToString() =>
            (string)PBXProjectType.GetMethod("WriteToString", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, null);

        public void Dispose() =>
            PBXProjectType.GetMethod("WriteToFile", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(_project, new[] { _projectPath });
    }
}
