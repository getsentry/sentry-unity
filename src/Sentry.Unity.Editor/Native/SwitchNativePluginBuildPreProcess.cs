using System;
using System.IO;
using System.Linq;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Manages native plugin stubs for Nintendo Switch builds.
/// </summary>
/// <remarks>
/// For Nintendo Switch, users must compile and provide their own static native Sentry library.
/// This preprocessor detects whether the user has provided the required native files and:
/// <list type="bullet">
/// <item>If all required files are present: disables the stub (real library will be linked)</item>
/// <item>If files are missing: enables the stub (provides no-op implementations to satisfy linker)</item>
/// <item>If files are partially present: warns the user about misconfiguration</item>
/// </list>
/// </remarks>
internal class SwitchNativePluginBuildPreProcess : IPreprocessBuildWithReport
{
    private static bool IsSwitchFamily(BuildTarget target) =>
        target == BuildTarget.Switch || IsSwitch2(target);

    /// <summary>
    /// <c>BuildTarget.Switch2</c> only exists in Unity 6000.3 and newer.
    /// </summary>
    private static bool IsSwitch2(BuildTarget target) =>
        string.Equals(target.ToString(), "Switch2", StringComparison.Ordinal);

    /// <summary>
    /// Both platforms share one stub, so the required libraries are what differ between them.
    /// </summary>
    private static string[] RequiredFilesFor(string targetDirectory)
    {
        return
        [
            $"Assets/Plugins/Sentry/{targetDirectory}/libsentry.a",
            $"Assets/Plugins/Sentry/{targetDirectory}/libzstd.a"
        ];
    }

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!IsSwitchFamily(report.summary.platform))
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

        ConfigureStub(logger, options?.SwitchNativeSupportEnabled ?? false, report.summary.platform);
    }

    internal static void ConfigureStub(IDiagnosticLogger logger, bool nativeSupportEnabled, BuildTarget target)
    {
        var requiredFiles = RequiredFilesFor(nameof(target));

        logger.LogDebug("{0} native support: checking for required files:\n{1}",
            target, string.Join("\n", requiredFiles.Select(f => $"  - {f}")));

        // One stub serves both platforms; the importer tracks compatibility per build target, so
        // enabling it for one does not affect the other.
        var stubPath = Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "Switch", "sentry_native_stubs.c");

        var importer = AssetImporter.GetAtPath(stubPath) as PluginImporter;
        if (importer == null)
        {
            logger.LogError("Failed to get PluginImporter for stub at '{0}'. Skipping stub configuration.", stubPath);
            return;
        }

        var existingFiles = requiredFiles.Where(File.Exists).ToList();
        var missingFiles = requiredFiles.Except(existingFiles).ToList();

        var someFilesPresent = existingFiles.Count > 0 && missingFiles.Count > 0;
        if (someFilesPresent)
        {
            logger.LogWarning(
                "{0} native support is partially configured. Missing files:\n{1}\n" +
                "Please add all required files to enable native support, or remove all files to fall back on no-op stubs.\n" +
                "Build sentry-switch and copy the libraries to the expected locations. " +
                "See: https://github.com/getsentry/sentry-switch",
                target, string.Join("\n", missingFiles.Select(f => $"  - {f}"))
            );
            return;
        }

        var allFilesPresent = missingFiles.Count == 0;
        if (allFilesPresent)
        {
            logger.LogInfo("{0} native libraries found:\n{1}",
                target, string.Join("\n", existingFiles.Select(f => $"  - {f}")));
            importer.SetCompatibleWithPlatform(target, false);
        }
        else
        {
            if (nativeSupportEnabled)
            {
                logger.LogWarning(
                    "{0} native support is enabled but required files are missing:\n{1}\n" +
                    "Build sentry-switch and copy the libraries to the expected locations. " +
                    "See: https://github.com/getsentry/sentry-switch",
                    target, string.Join("\n", missingFiles.Select(f => $"  - {f}"))
                );
            }
            else
            {
                logger.LogDebug("{0} native support is disabled. Enabling stubs (native calls will be no-op).", target);
            }
            importer.SetCompatibleWithPlatform(target, true);
        }

        importer.SaveAndReimport();
    }
}
