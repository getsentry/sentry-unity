using System.IO;
using System.Text.RegularExpressions;
using Sentry.Extensibility;

namespace Sentry.Unity.Editor.Android;

internal class GradleSetup
{
    private readonly IDiagnosticLogger _logger;

    private const string AndroidMarker = "android {";
    private const string SdkDependenciesFull = SdkDependencies + "\n}\n\n" + AndroidMarker;
    private const string SdkDependenciesWithReplayFull = SdkDependencies + ReplayDependencies + "\n}\n\n" + AndroidMarker;

    public const string SdkDependencies = @"dependencies {
    implementation(name: 'sentry-android-ndk-release', ext:'aar')
    implementation(name: 'sentry-android-core-release', ext:'aar')
    implementation(name: 'sentry-native-ndk-release', ext:'aar')";

    // Session replay is written in Kotlin. Local AARs don't bring their dependencies, so the Kotlin standard library
    // comes from Maven, at the version sentry-android-replay builds with.
    public const string ReplayDependencies = @"
    implementation(name: 'sentry-android-replay-release', ext:'aar')
    implementation 'org.jetbrains.kotlin:kotlin-stdlib:1.9.24'";
    public const string DependenciesAddedMessage = "The Sentry Gradle dependencies have already been added.";
    private readonly string _unityLibraryGradle;
    private readonly bool _sessionReplayEnabled;

    public GradleSetup(IDiagnosticLogger logger, string gradleProjectPath, bool sessionReplayEnabled = false)
    {
        _logger = logger;
        _unityLibraryGradle = Path.Combine(gradleProjectPath, "unityLibrary", "build.gradle");
        _sessionReplayEnabled = sessionReplayEnabled;
    }

    private string Dependencies => _sessionReplayEnabled ? SdkDependenciesWithReplayFull : SdkDependenciesFull;
    private string OtherDependencies => _sessionReplayEnabled ? SdkDependenciesFull : SdkDependenciesWithReplayFull;

    public void UpdateGradleProject()
    {
        _logger.LogInfo("Adding Sentry to the gradle project.");
        var fileContent = LoadGradleScript(_unityLibraryGradle);
        fileContent = AddSentryToGradle(fileContent);
        File.WriteAllText(_unityLibraryGradle, fileContent);
    }

    public void ClearGradleProject()
    {
        _logger.LogInfo("Removing Sentry from the gradle project.");
        var fileContent = LoadGradleScript(_unityLibraryGradle);

        // A persisted Gradle project can hold either variant of the dependencies.
        if (!fileContent.Contains(SdkDependenciesFull) && !fileContent.Contains(SdkDependenciesWithReplayFull))
        {
            _logger.LogDebug("The Sentry Gradle dependencies have already been removed.");
            return;
        }

        fileContent = fileContent
            .Replace(SdkDependenciesFull, AndroidMarker)
            .Replace(SdkDependenciesWithReplayFull, AndroidMarker);
        File.WriteAllText(_unityLibraryGradle, fileContent);
    }

    public string AddSentryToGradle(string fileContent)
    {
        if (fileContent.Contains(Dependencies))
        {
            _logger.LogDebug(DependenciesAddedMessage);
            return fileContent;
        }

        // A persisted Gradle project may hold the dependencies from a build with session replay toggled.
        if (fileContent.Contains(OtherDependencies))
        {
            return fileContent.Replace(OtherDependencies, Dependencies);
        }

        var regex = new Regex(Regex.Escape(AndroidMarker));
        return regex.Replace(fileContent, Dependencies, 1);
    }

    internal static string LoadGradleScript(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Failed to find the gradle config.", path);
        }
        return File.ReadAllText(path);
    }
}
