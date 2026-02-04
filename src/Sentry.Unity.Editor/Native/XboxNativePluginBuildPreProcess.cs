using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Validates native plugin presence for Xbox builds.
/// </summary>
/// <remarks>
/// For Xbox, users must compile and provide their own native Sentry library.
/// This preprocessor detects whether the user has provided the required native files and
/// logs an error if native support is enabled but the files are missing.
/// The native SDK is loaded dynamically at runtime and failures are handled gracefully.
/// Supports both Xbox Series X/S (GameCoreScarlett) and Xbox One (GameCoreXboxOne).
/// </remarks>
internal class XboxNativePluginBuildPreProcess : IPreprocessBuildWithReport
{
    private const string XsxRequiredFile = "Assets/Plugins/Sentry/XSX/sentry.dll";
    private const string Xb1RequiredFile = "Assets/Plugins/Sentry/XB1/sentry.dll";

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.GameCoreXboxSeries &&
            report.summary.platform != BuildTarget.GameCoreXboxOne)
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

        var requiredFile = report.summary.platform == BuildTarget.GameCoreXboxSeries
            ? XsxRequiredFile
            : Xb1RequiredFile;

        var platformName = report.summary.platform == BuildTarget.GameCoreXboxSeries
            ? "Xbox Series X|S"
            : "Xbox One";

        ValidateNativePlugin(logger, options?.XboxNativeSupportEnabled ?? true, requiredFile, platformName);
    }

    internal static void ValidateNativePlugin(IDiagnosticLogger logger, bool nativeSupportEnabled,
        string requiredFile, string platformName)
    {
        logger.LogDebug($"{platformName} native support: checking for required file:\n  - {requiredFile}");

        if (File.Exists(requiredFile))
        {
            logger.LogInfo($"{platformName} native library found at '{requiredFile}'.");
        }
        else if (nativeSupportEnabled)
        {
            logger.LogError(
                $"{platformName} native support is enabled but the required file is missing:\n" +
                $"  - {requiredFile}\n" +
                "Build sentry-xbox and copy the library to the expected location. " +
                "See: https://github.com/getsentry/sentry-xbox"
            );
        }
        else
        {
            logger.LogDebug($"{platformName} native support is disabled and native library is not present.");
        }
    }
}
