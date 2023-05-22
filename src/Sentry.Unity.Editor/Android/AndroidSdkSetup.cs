using System.IO;
using Sentry.Extensibility;

namespace Sentry.Unity.Editor.Android
{
    internal class AndroidSdkSetup
    {
        private readonly IDiagnosticLogger _logger;
        private readonly string _androidSdkPath;
        private readonly string _targetAndroidSdkPath;

        public AndroidSdkSetup(IDiagnosticLogger logger, string unityProjectPath, string gradleProjectPath)
        {
            _logger = logger;
            _androidSdkPath = Path.Combine(unityProjectPath, "Packages", SentryPackageInfo.GetName(), "Plugins", "Android", "Sentry~");
            _targetAndroidSdkPath = Path.Combine(gradleProjectPath, "unityLibrary", "android-sdk-repository");
        }

        internal void AddAndroidSdk()
        {
            if (Directory.Exists(_targetAndroidSdkPath))
            {
                _logger.LogDebug("Android SDK already detected at '{0}'. Skip copying.", _targetAndroidSdkPath);
                return;
            }

            if (!Directory.Exists(_androidSdkPath))
            {
                throw new DirectoryNotFoundException($"Failed to find the Android SDK at '{_androidSdkPath}'.");
            }

            _logger.LogInfo("Copying the Android SDK to '{0}'.", _targetAndroidSdkPath);
            CopyDirectory(_androidSdkPath, _targetAndroidSdkPath);
        }

        public void RemoveAndroidSdk()
        {
            if (Directory.Exists(_targetAndroidSdkPath))
            {
                _logger.LogInfo("Removing the Android SDK from the output project.");
                Directory.Delete(_targetAndroidSdkPath, true);
            }
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            var directory = new DirectoryInfo(sourceDirectory);
            if (!directory.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {directory.FullName}");
            }

            var subDirectories = directory.GetDirectories();
            Directory.CreateDirectory(destinationDirectory);

            foreach (var file in directory.GetFiles())
            {
                var targetFilePath = Path.Combine(destinationDirectory, file.Name);
                file.CopyTo(targetFilePath);
            }

            foreach (var subDirectory in subDirectories)
            {
                var newDestinationDir = Path.Combine(destinationDirectory, subDirectory.Name);
                CopyDirectory(subDirectory.FullName, newDestinationDir);
            }
        }
    }
}
