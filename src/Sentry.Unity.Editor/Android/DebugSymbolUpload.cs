using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Editor.Android
{
    internal class DebugSymbolUpload
    {
        private IDiagnosticLogger _logger;

        public DebugSymbolUpload(IDiagnosticLogger logger)
        {
            _logger = logger;
        }

        public string[] GetDefaultSymbolPaths(string unityProjectPath, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            // Starting from 2021.2 Unity caches the build inside the 'Library' instead of 'Temp'
            if (application.UnityVersion.StartsWith("2022")
                || application.UnityVersion.StartsWith("2021.3")
                || application.UnityVersion.StartsWith("2021.2"))
            {
                _logger.LogInfo("Unity version 2021.2 or newer detected. Root for symbols upload: 'Library'.");
                return new[]
                {
                    Path.Combine(unityProjectPath, "Library", "Bee"),
                    Path.Combine(unityProjectPath, "Library", "Android")
                };
            }

            _logger.LogInfo("Unity version 2021.1 or older detected. Root for symbols upload: 'Temp'.");
            return new[]
            {
                Path.Combine(unityProjectPath, "Temp", "StagingArea"),
                Path.Combine(unityProjectPath, "Temp", "gradleOut")
            };
        }

        public void AppendUploadToGradleFile(string sentryCliPath, string gradleProjectPath, string[] symbolsDirectoryPaths, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            _logger.LogDebug("Appending debug symbols upload task to gradle file.");

            // Gradle doesn't support backslashes on path (Windows) so converting to forward slashes
            if (application.Platform == RuntimePlatform.WindowsEditor)
            {
                _logger.LogDebug("Converting backslashes to forward slashes.");

                sentryCliPath = sentryCliPath.Replace(@"\", "/");
                for (var i = 0; i < symbolsDirectoryPaths.Length; i++)
                {
                    symbolsDirectoryPaths[i] = symbolsDirectoryPaths[i].Replace(@"\", "/");
                }
            }

            if (!File.Exists(sentryCliPath))
            {
                throw new FileNotFoundException("Failed to find sentry-cli", sentryCliPath);
            }

            var gradleFilePath = Path.Combine(gradleProjectPath, "build.gradle");
            if (!File.Exists(gradleFilePath))
            {
                throw new FileNotFoundException("Failed to find 'build.gradle'", gradleProjectPath);
            }

            var pathsAsArgument = "";
            foreach (var symbolsDirectoryPath in symbolsDirectoryPaths)
            {
                if (Directory.Exists(symbolsDirectoryPath))
                {
                    pathsAsArgument += $"\"{symbolsDirectoryPath}\",";
                }
                else
                {
                    throw new DirectoryNotFoundException($"Failed to find the symbols directory at {symbolsDirectoryPath}");
                }
            }

            using var streamWriter = File.AppendText(gradleFilePath);
            streamWriter.Write($@"
// Credentials and project settings information are stored in the sentry.properties file
gradle.taskGraph.whenReady {{
    gradle.taskGraph.allTasks[-1].doLast {{
        println 'Uploading symbols to Sentry'
        exec {{
            environment ""SENTRY_PROPERTIES"", ""./sentry.properties""
            executable ""{sentryCliPath}""
            args = [""upload-dif"", {pathsAsArgument}]
        }}
    }}
}}");
        }

        internal static void CopySymbols(string sourcePath, string targetPath)
        {
            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (var newPath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}
