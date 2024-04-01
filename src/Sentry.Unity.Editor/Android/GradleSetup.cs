using System.IO;
using System.Text.RegularExpressions;
using Sentry.Extensibility;

namespace Sentry.Unity.Editor.Android
{
    internal class GradleSetup
    {
        private readonly IDiagnosticLogger _logger;

        private const string AndroidMarker = "android {";
        private const string SdkDependenciesFull = SdkDependencies + "\n}\n\n" + AndroidMarker;

        public const string SdkDependencies = "dependencies {\n\timplementation(name: 'sentry-android-ndk-release', ext:'aar')\n\timplementation(name: 'sentry-android-core-release', ext:'aar')";
        public const string DependenciesAddedMessage = "The Sentry Gradle dependencies have already been added.";
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
            fileContent = AddSentryToGradle(fileContent);
            File.WriteAllText(_unityLibraryGradle, fileContent);
        }

        public void ClearGradleProject()
        {
            _logger.LogDebug("Removing Sentry from the gradle project.");
            var fileContent = LoadGradleScript(_unityLibraryGradle);

            if (!fileContent.Contains(SdkDependenciesFull))
            {
                _logger.LogDebug("The Sentry Gradle dependencies have already been removed.");
                return;
            }

            fileContent = fileContent.Replace(SdkDependenciesFull, AndroidMarker);
            File.WriteAllText(_unityLibraryGradle, fileContent);
        }

        public string AddSentryToGradle(string fileContent)
        {
            if (fileContent.Contains(SdkDependenciesFull))
            {
                _logger.LogDebug(DependenciesAddedMessage);
                return fileContent;
            }

            var regex = new Regex(Regex.Escape(AndroidMarker));
            return regex.Replace(fileContent, SdkDependenciesFull, 1);
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
