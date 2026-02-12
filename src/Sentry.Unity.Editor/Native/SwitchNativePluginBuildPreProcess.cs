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
    private static readonly string[] RequiredFiles =
    {
        "Assets/Plugins/Sentry/Switch/libsentry.a",
        "Assets/Plugins/Sentry/Switch/libzstd.a",
    };

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Switch)
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

        ConfigureStub(logger, options?.SwitchNativeSupportEnabled ?? false);
    }

    internal static void ConfigureStub(IDiagnosticLogger logger, bool nativeSupportEnabled)
    {
        logger.LogDebug("Switch native support: checking for required files:\n{0}",
            string.Join("\n", RequiredFiles.Select(f => $"  - {f}")));

        var stubPath = Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", "Switch", "sentry_native_stubs.c");

        var importer = AssetImporter.GetAtPath(stubPath) as PluginImporter;
        if (importer == null)
        {
            logger.LogError("Failed to get PluginImporter for stub at '{0}'. Skipping stub configuration.", stubPath);
            return;
        }

        var existingFiles = RequiredFiles.Where(File.Exists).ToList();
        var missingFiles = RequiredFiles.Except(existingFiles).ToList();

        var someFilesPresent = existingFiles.Count > 0 && missingFiles.Count > 0;
        if (someFilesPresent)
        {
            logger.LogError(
                "Switch native support is partially configured. Missing files:\n{0}\n" +
                "Please add all required files to enable native support, or remove all files to fall back on no-op stubs.\n" +
                "Build sentry-switch and copy the libraries to the expected locations. " +
                "See: https://github.com/getsentry/sentry-switch",
                string.Join("\n", missingFiles.Select(f => $"  - {f}"))
            );
            return;
        }

        var allFilesPresent = missingFiles.Count == 0;
        if (allFilesPresent)
        {
            logger.LogInfo("Switch native libraries found:\n{0}",
                string.Join("\n", existingFiles.Select(f => $"  - {f}")));
            importer.SetCompatibleWithPlatform(BuildTarget.Switch, false);
        }
        else
        {
            if (nativeSupportEnabled)
            {
                logger.LogWarning(
                    "Switch native support is enabled but required files are missing:\n{0}\n" +
                    "Build sentry-switch and copy the libraries to the expected locations. " +
                    "See: https://github.com/getsentry/sentry-switch",
                    string.Join("\n", missingFiles.Select(f => $"  - {f}"))
                );
            }
            else
            {
                logger.LogDebug("Switch native support is disabled. Enabling stubs (native calls will be no-op).");
            }
            importer.SetCompatibleWithPlatform(BuildTarget.Switch, true);
        }

        importer.SaveAndReimport();
    }
}
