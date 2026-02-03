using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Manages native plugin stubs for platforms that require user-provided native libraries.
/// </summary>
/// <remarks>
/// For platforms like Nintendo Switch, users must compile and provide their own native Sentry library.
/// This preprocessor detects whether the user has provided the required native files and:
/// <list type="bullet">
/// <item>If all required files are present: disables the stub (real library will be linked)</item>
/// <item>If files are missing: enables the stub (provides no-op implementations to satisfy linker)</item>
/// <item>If files are partially present: warns the user about misconfiguration</item>
/// </list>
/// </remarks>
internal class NativePluginBuildPreProcess : IPreprocessBuildWithReport
{
    private const string relativeStubPath = "Plugins/sentry_native_stubs.c";

    /// <summary>
    /// Configuration for a platform's native plugin stub.
    /// </summary>
    internal sealed class PlatformNativeConfig
    {
        public string PlatformName { get; }
        public string[] RequiredFiles { get; }
        public BuildTarget BuildTarget { get; }

        public PlatformNativeConfig(string platformName, string[] requiredFiles, BuildTarget buildTarget)
        {
            PlatformName = platformName;
            RequiredFiles = requiredFiles;
            BuildTarget = buildTarget;
        }
    }

    /// <summary>
    /// Platform configurations for native plugin stub management.
    /// </summary>
    private static readonly Dictionary<BuildTarget, PlatformNativeConfig> PlatformConfigs = new()
    {
        [BuildTarget.Switch] = new PlatformNativeConfig(
            platformName: "Switch",
            requiredFiles: new[]
            {
                "Assets/Plugins/Sentry/Switch/libsentry.a",
                "Assets/Plugins/Sentry/Switch/libzstd.a",
            },
            buildTarget: BuildTarget.Switch
        ),
        [BuildTarget.PS5] = new PlatformNativeConfig(
            platformName: "PlayStation",
            requiredFiles: new[]
            {
                "Assets/Plugins/Sentry/PS5/sentry.prx",
            },
            buildTarget: BuildTarget.PS5
        ),
        [BuildTarget.GameCoreXboxSeries] = new PlatformNativeConfig(
            platformName: "Xbox Series X/S",
            requiredFiles: new[]
            {
                "Assets/Plugins/Sentry/XSX/sentry.dll",
            },
            buildTarget: BuildTarget.GameCoreXboxSeries
        ),
        [BuildTarget.GameCoreXboxOne] = new PlatformNativeConfig(
            platformName: "Xbox One",
            requiredFiles: new[]
            {
                "Assets/Plugins/Sentry/XB1/sentry.dll",
            },
            buildTarget: BuildTarget.GameCoreXboxOne
        ),
    };

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!PlatformConfigs.TryGetValue(report.summary.platform, out var config))
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

        ConfigurePlatformStub(config, logger);
    }

    internal static void ConfigurePlatformStub(PlatformNativeConfig config, IDiagnosticLogger logger)
    {
        var stubPath = Path.Combine("Packages", SentryPackageInfo.GetName(), relativeStubPath);
        if (string.IsNullOrEmpty(stubPath))
        {
            logger.LogError("Stubs for 'sentry_native' not found at '{0}'. Skipping stub configuration.", stubPath);
            return;
        }

        var importer = AssetImporter.GetAtPath(stubPath) as PluginImporter;
        if (importer == null)
        {
            logger.LogError("Failed to get PluginImporter for stub at '{0}'. Skipping stub configuration.", stubPath);
            return;
        }

        var existingFiles = config.RequiredFiles.Where(File.Exists).ToList();
        var missingFiles = config.RequiredFiles.Except(existingFiles).ToList();

        var someFilesPresent = existingFiles.Count > 0 && missingFiles.Count > 0;
        if (someFilesPresent)
        {
            logger.LogError(
                "The native support is partially configured. Missing files:\n{0}\n" +
                "Please add all required files to enable native support, or remove all files to fall back on no-op stubs.",
                string.Join("\n", missingFiles.Select(f => $"  - {f}"))
            );
            return;
        }

        var allFilesPresent = missingFiles.Count == 0;
        if (allFilesPresent)
        {
            logger.LogInfo("{0} native libs found. Disabling stubs, native support enabled.",
                config.PlatformName);
            importer.SetCompatibleWithPlatform(config.BuildTarget, false);
        }
        else
        {
            logger.LogInfo("{0} native libs not found. Enabling stubs (native calls will be no-op).",
                config.PlatformName);
            importer.SetCompatibleWithPlatform(config.BuildTarget, true);
        }

        importer.SaveAndReimport();
    }
}
