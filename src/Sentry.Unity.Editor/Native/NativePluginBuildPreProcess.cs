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
        /// <summary>
        /// Path to the platform's sentry_utils.c file (relative to package root).
        /// When stubs are enabled, this utility plugin should be disabled to avoid duplicate symbols.
        /// When stubs are disabled (real native lib present), this should be enabled.
        /// </summary>
        public string? UtilityPluginPath { get; }

        public PlatformNativeConfig(string platformName, string[] requiredFiles, BuildTarget buildTarget,
            string? utilityPluginPath = null)
        {
            PlatformName = platformName;
            RequiredFiles = requiredFiles;
            BuildTarget = buildTarget;
            UtilityPluginPath = utilityPluginPath;
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
            // Switch has no utility plugin - vsnprintf_sentry comes from user's native lib or stubs
        ),
        [BuildTarget.PS5] = new PlatformNativeConfig(
            platformName: "PlayStation",
            requiredFiles: new[]
            {
                "Assets/Plugins/Sentry/PS5/sentry.prx",
            },
            buildTarget: BuildTarget.PS5,
            utilityPluginPath: "Plugins/PS5/sentry_utils.c"
        ),
        [BuildTarget.GameCoreXboxSeries] = new PlatformNativeConfig(
            platformName: "Xbox Series X/S",
            requiredFiles: new[]
            {
                "Assets/Plugins/Sentry/XSX/sentry.dll",
            },
            buildTarget: BuildTarget.GameCoreXboxSeries,
            utilityPluginPath: "Plugins/Xbox/sentry_utils.c"
        ),
        [BuildTarget.GameCoreXboxOne] = new PlatformNativeConfig(
            platformName: "Xbox One",
            requiredFiles: new[]
            {
                "Assets/Plugins/Sentry/XB1/sentry.dll",
            },
            buildTarget: BuildTarget.GameCoreXboxOne,
            utilityPluginPath: "Plugins/Xbox/sentry_utils.c"
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

        var stubImporter = AssetImporter.GetAtPath(stubPath) as PluginImporter;
        if (stubImporter == null)
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
            // Native libs found - disable stubs, enable utility plugin
            logger.LogInfo("{0} native libs found. Disabling stubs, native support enabled.",
                config.PlatformName);
            stubImporter.SetCompatibleWithPlatform(config.BuildTarget, false);
            ConfigureUtilityPlugin(config, logger, enabled: true);
        }
        else
        {
            // Native libs not found - enable stubs, disable utility plugin (to avoid duplicate symbols)
            logger.LogInfo("{0} native libs not found. Enabling stubs (native calls will be no-op).",
                config.PlatformName);
            stubImporter.SetCompatibleWithPlatform(config.BuildTarget, true);
            ConfigureUtilityPlugin(config, logger, enabled: false);
        }

        stubImporter.SaveAndReimport();
    }

    private static void ConfigureUtilityPlugin(PlatformNativeConfig config, IDiagnosticLogger logger, bool enabled)
    {
        if (config.UtilityPluginPath == null)
        {
            return;
        }

        var utilityPath = Path.Combine("Packages", SentryPackageInfo.GetName(), config.UtilityPluginPath);
        var utilityImporter = AssetImporter.GetAtPath(utilityPath) as PluginImporter;

        if (utilityImporter == null)
        {
            logger.LogWarning("Failed to get PluginImporter for utility plugin at '{0}'.", utilityPath);
            return;
        }

        logger.LogDebug("{0} sentry_utils.c for {1}.", enabled ? "Enabling" : "Disabling", config.PlatformName);
        utilityImporter.SetCompatibleWithPlatform(config.BuildTarget, enabled);
        utilityImporter.SaveAndReimport();
    }
}
