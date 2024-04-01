using System.IO;
using System.Text.RegularExpressions;
using Sentry.Extensibility;

namespace Sentry.Unity.Editor.Android
{
    internal class GradleSetup
    {
        private readonly IDiagnosticLogger _logger;

        private const string AndroidMarker = "android {";
        private const string DependenciesMarker = "dependencies {";
        private const string SdkDependenciesFull = SdkDependencies + "\n}\n\n" + AndroidMarker;

        public const string SdkDependencies = DependenciesMarker + "\n\timplementation(name: 'sentry-android-ndk-release', ext:'aar')\n\timplementation(name: 'sentry-android-core-release', ext:'aar')";

        private readonly string _unityLibraryGradle;

        public GradleSetup(IDiagnosticLogger logger, string gradleProjectPath)
        {
            _logger = logger;
            _unityLibraryGradle = Path.Combine(gradleProjectPath, "unityLibrary", "build.gradle");
        }

        public void UpdateGradleProject()
        {
            _logger.LogDebug("Adding Sentry to the gradle project.");
            var fileContent = LoadGradleScript(_unityLibraryGradle);
            fileContent = ReplaceGradleContents(fileContent);
            File.WriteAllText(_unityLibraryGradle, fileContent);
        }

        public void ClearGradleProject()
        {
            _logger.LogDebug("Removing Sentry from the gradle project.");
            var fileContent = LoadGradleScript(_unityLibraryGradle);
            var hasFullDependencies = fileContent.Contains(SdkDependenciesFull);
            var textToFind = hasFullDependencies ? SdkDependenciesFull : SdkDependencies;
            var textToInsert = hasFullDependencies ? AndroidMarker : DependenciesMarker;

            if (!fileContent.Contains(textToFind))
            {
                _logger.LogDebug("The Sentry Gradle dependencies have already been removed.");
                return;
            }

            fileContent = fileContent.Replace(textToFind, textToInsert);
            File.WriteAllText(_unityLibraryGradle, fileContent);
        }

        public string ReplaceGradleContents(string fileContent)
        {
            var hasDependenciesSection = fileContent.Contains(DependenciesMarker);
            var textToInsert = hasDependenciesSection ? SdkDependencies : SdkDependenciesFull;
            var textToFind = hasDependenciesSection ? DependenciesMarker : AndroidMarker;

            if (fileContent.Contains(textToInsert))
            {
                _logger.LogDebug("The Sentry Gradle dependencies have already been added.");
                return fileContent;
            }

            if (!fileContent.Contains(textToFind))
            {
                _logger.LogError("Could not find marker: " + textToFind +
                                 "to add Sentry Gradle dependencies. Please, make sure your mainGradle.template contains either dependencies or android section.");
                return fileContent;
            }

            var regex = new Regex(Regex.Escape(textToFind));
            return regex.Replace(fileContent, textToInsert, 1);
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
}
