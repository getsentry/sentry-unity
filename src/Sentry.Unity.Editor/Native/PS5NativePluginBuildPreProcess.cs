using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Validates native plugin presence for PlayStation 5 builds.
/// </summary>
/// <remarks>
/// For PlayStation 5, users must compile and provide their own native Sentry library.
/// This preprocessor detects whether the user has provided the required native files and
/// logs an error if native support is enabled but the files are missing.
/// The native SDK is loaded dynamically at runtime and failures are handled gracefully.
/// </remarks>
internal class PS5NativePluginBuildPreProcess : IPreprocessBuildWithReport
{
    private const string RequiredFile = "Assets/Plugins/Sentry/PS5/sentry.prx";

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.PS5)
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

        ValidateNativePlugin(logger, options?.PlayStationNativeSupportEnabled ?? true);
    }

    internal static void ValidateNativePlugin(IDiagnosticLogger logger, bool nativeSupportEnabled)
    {
        logger.LogDebug("PS5 native support: checking for required file:\n  - {0}", RequiredFile);

        if (File.Exists(RequiredFile))
        {
            logger.LogInfo("PS5 native library found at '{0}'.", RequiredFile);
        }
        else if (nativeSupportEnabled)
        {
            logger.LogError(
                "PS5 native support is enabled but the required file is missing:\n" +
                "  - {0}\n" +
                "Build sentry-playstation and copy the library to the expected location. " +
                "See: https://github.com/getsentry/sentry-playstation",
                RequiredFile
            );
        }
        else
        {
            logger.LogDebug("PS5 native support is disabled and native library is not present.");
        }
    }
}
