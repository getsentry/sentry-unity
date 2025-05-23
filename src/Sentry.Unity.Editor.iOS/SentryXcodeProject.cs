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

namespace Sentry.Unity.Editor.iOS;

internal class SentryXcodeProject : IDisposable
{
    internal const string FrameworkName = "Sentry.xcframework";
    internal const string BridgeName = "SentryNativeBridge.m";
    internal const string NoOpBridgeName = "SentryNativeBridgeNoOp.m";
    internal const string OptionsName = "SentryOptions.m";
    internal const string SymbolUploadPhaseName = "SymbolUpload";

    internal const string SymbolUploadPropertyName = "SENTRY_CLI_UPLOAD_SYMBOLS";
    internal const string IncludeSourcesPropertyName = "SENTRY_CLI_INCLUDE_SOURCES";
    internal const string AllowFailurePropertyName = "SENTRY_CLI_ALLOW_FAILURE";
    internal const string PrintLogsPropertyName = "SENTRY_CLI_PRINT_LOGS";

    internal static readonly string MainPath = Path.Combine("MainApp", "main.mm");
    private readonly string _optionsPath = Path.Combine("MainApp", OptionsName);
    private readonly string _uploadScript = @"export SENTRY_PROPERTIES=sentry.properties
if [ ""${" + SymbolUploadPropertyName + @"}"" == ""YES"" ]; then
    echo ""Uploading debug symbols and bcsymbolmaps.""
    echo ""Writing logs to './sentry-symbols-upload.log'""

    ARGS=""--il2cpp-mapping""

    if [ ""${" + IncludeSourcesPropertyName + @"}"" == ""YES"" ]; then
        ARGS=""$ARGS --include-sources""
    fi

    if [ ""${" + AllowFailurePropertyName + @"}"" == ""YES"" ]; then
        ARGS=""$ARGS --allow-failure""
    fi

    if [ ""${" + PrintLogsPropertyName + @"}"" == ""YES"" ]; then
        ./" + SentryCli.SentryCliMacOS + @" debug-files upload $ARGS $BUILT_PRODUCTS_DIR 2>&1 | tee ./sentry-symbols-upload.log
    else
        ./" + SentryCli.SentryCliMacOS + @" debug-files upload $ARGS $BUILT_PRODUCTS_DIR 2> ./sentry-symbols-upload.log
    fi
else
    echo ""Sentry symbol upload has been disabled via build setting '" + SymbolUploadPropertyName + @"'.""
fi";

    private readonly IDiagnosticLogger? _logger = null;
    private readonly Type _pbxProjectType = null!;              // Set in constructor or throws
    private readonly Type _pbxProjectExtensionsType = null!;    // Set in constructor or throws
    private readonly object _project = null!;                   // Set in constructor or throws

    private readonly string _projectRoot;
    private readonly string _projectPath;

    private string _mainTargetGuid = null!;             // Set when opening the project
    private string _unityFrameworkTargetGuid = null!;   // Set when opening the project

    internal SentryXcodeProject(string projectRoot, IDiagnosticLogger? logger = null)
    {
        _logger = logger;
        var xcodeAssembly = Assembly.Load("UnityEditor.iOS.Extensions.Xcode");
        _pbxProjectType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.PBXProject");
        _pbxProjectExtensionsType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.Extensions.PBXProjectExtensions");

        _project = Activator.CreateInstance(_pbxProjectType);

        _projectRoot = projectRoot;
        _projectPath = (string)_pbxProjectType.GetMethod("GetPBXProjectPath", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new[] { _projectRoot });
    }

    public static SentryXcodeProject Open(string path, IDiagnosticLogger? logger = null)
    {
        var xcodeProject = new SentryXcodeProject(path, logger);
        xcodeProject.ReadFromProjectFile();

        return xcodeProject;
    }

    internal void ReadFromProjectFile()
    {
        _logger?.LogInfo("Reading the Xcode project file.");

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

        _logger?.LogDebug("Successfully read the Xcode project file.");
    }

    public void AddSentryFramework()
    {
        _logger?.LogInfo("Adding the build properties and flags.");
        AddBuildProperties();

        var relativeFrameworkPath = Path.Combine("Frameworks", FrameworkName);

        var hasFileMethod = _pbxProjectType.GetMethod("ContainsFileByProjectPath", new[] { typeof(string) });
        if (hasFileMethod != null)
        {
            var hasAddedFramework = (bool)hasFileMethod.Invoke(_project, new object[] { relativeFrameworkPath });
            if (hasAddedFramework)
            {
                _logger?.LogDebug("Skipping. The Sentry framework has already been added to the project.");
                return;
            }
        }

        _logger?.LogInfo("Adding the Sentry framework.");

        var frameworkGuid = (string)_pbxProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(_project, new object[] { relativeFrameworkPath, relativeFrameworkPath, 1 }); // 1 is PBXSourceTree.Source

        // Embedding the framework because it's dynamic and needed at runtime
        _pbxProjectExtensionsType.GetMethod("AddFileToEmbedFrameworks", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object?[] { _project, _mainTargetGuid, frameworkGuid, null });

        // Getting the Link With Binary phase
        var getBuildPhaseMethod = _pbxProjectType.GetMethod("GetFrameworksBuildPhaseByTarget", new[] { typeof(string) });
        var mainBuildPhaseGuid = (string)getBuildPhaseMethod.Invoke(_project, new object[] { _mainTargetGuid });
        var unityFrameworkBuildPhaseGuid = (string)getBuildPhaseMethod.Invoke(_project, new object[] { _unityFrameworkTargetGuid });

        // Linking With Binary
        var addFileToBuildSectionMethod = _pbxProjectType.GetMethod("AddFileToBuildSection", new[] { typeof(string), typeof(string), typeof(string) });
        addFileToBuildSectionMethod.Invoke(_project, new object[] { _mainTargetGuid, mainBuildPhaseGuid, frameworkGuid });
        addFileToBuildSectionMethod.Invoke(_project, new object[] { _unityFrameworkTargetGuid, unityFrameworkBuildPhaseGuid, frameworkGuid });

        _logger?.LogDebug("Successfully added the Sentry framework.");
    }

    private void AddBuildProperties()
    {
        SetSearchPathBuildProperty("$(inherited)");
        SetSearchPathBuildProperty("$(PROJECT_DIR)/Frameworks/");

        var setBuildPropertyMethod = _pbxProjectType.GetMethod("SetBuildProperty", new[] { typeof(string), typeof(string), typeof(string) });
        setBuildPropertyMethod.Invoke(_project, new object[] { _mainTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" });
        setBuildPropertyMethod.Invoke(_project, new object[] { _unityFrameworkTargetGuid, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym" });

        _pbxProjectType.GetMethod("AddBuildProperty", new[] { typeof(string), typeof(string), typeof(string) })
            .Invoke(_project, new object[] { _mainTargetGuid, "OTHER_LDFLAGS", "-ObjC" });
    }

    public void AddSentryNativeBridge()
    {
        _logger?.LogInfo("Adding the Sentry Native Bridge.");

        var relativeBridgePath = Path.Combine("Libraries", SentryPackageInfo.GetName(), BridgeName);
        var bridgeGuid = (string)_pbxProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(_project, new object[] { relativeBridgePath, relativeBridgePath, 1 }); // 1 is PBXSourceTree.Source

        _pbxProjectType.GetMethod("AddFileToBuild", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(_project, new[] { _unityFrameworkTargetGuid, bridgeGuid });

        _logger?.LogDebug("Successfully added the Sentry Native Bridge.");
    }

    // Used for testing
    internal void SetSearchPathBuildProperty(string path)
    {
        _pbxProjectType.GetMethod("AddBuildProperty", new[] { typeof(string[]), typeof(string), typeof(string) })
            .Invoke(_project, new object[] { new[] { _mainTargetGuid, _unityFrameworkTargetGuid }, "FRAMEWORK_SEARCH_PATHS", path });
    }

    public void AddBuildPhaseSymbolUpload(SentryCliOptions sentryCliOptions)
    {
        _logger?.LogInfo("Setting up Sentry CLI build properties and upload script.");

        _pbxProjectType.GetMethod("SetBuildProperty", new[] { typeof(string), typeof(string), typeof(string) })
            .Invoke(_project, new object[] { _mainTargetGuid, SymbolUploadPropertyName, sentryCliOptions.UploadSymbols ? "YES" : "NO" });

        // Set additional upload properties
        _pbxProjectType.GetMethod("SetBuildProperty", new[] { typeof(string), typeof(string), typeof(string) })
            .Invoke(_project, new object[] { _mainTargetGuid, IncludeSourcesPropertyName, sentryCliOptions.UploadSources ? "YES" : "NO" });

        _pbxProjectType.GetMethod("SetBuildProperty", new[] { typeof(string), typeof(string), typeof(string) })
            .Invoke(_project, new object[] { _mainTargetGuid, AllowFailurePropertyName, sentryCliOptions.IgnoreCliErrors ? "YES" : "NO" });

        _pbxProjectType.GetMethod("SetBuildProperty", new[] { typeof(string), typeof(string), typeof(string) })
            .Invoke(_project, new object[] { _mainTargetGuid, PrintLogsPropertyName, sentryCliOptions.IgnoreCliErrors ? "NO" : "YES" });

        if (MainTargetContainsSymbolUploadBuildPhase())
        {
            _logger?.LogInfo("Success. Build phase '{0}' was already added.", SymbolUploadPhaseName);
            return;
        }

        _logger?.LogDebug("Adding the upload script to {0}.", SymbolUploadPhaseName);

        _pbxProjectType.GetMethod("AddShellScriptBuildPhase", new[] { typeof(string), typeof(string), typeof(string), typeof(string) })
                .Invoke(_project, new object[] { _mainTargetGuid, SymbolUploadPhaseName, "/bin/sh", _uploadScript });

        _logger?.LogInfo("Success. Added automated debug symbol upload script to build phase.");
    }

    public void AddNativeOptions(SentryUnityOptions options, Action<string, SentryUnityOptions> nativeOptionFileCreation)
    {
        _logger?.LogInfo("Adding native options.");

        nativeOptionFileCreation.Invoke(Path.Combine(_projectRoot, _optionsPath), options);
        _pbxProjectType.GetMethod("AddFile", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(_project, new object[] { _optionsPath, _optionsPath, 1 }); // 1 is PBXSourceTree.Source

        _logger?.LogDebug("Successfully added native options.");
    }

    public void AddSentryToMain(SentryUnityOptions options) =>
        NativeMain.AddSentry(Path.Combine(_projectRoot, MainPath), options.DiagnosticLogger);

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
