using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEditor;

namespace Sentry.Unity.Editor.Android
{
    internal class DebugSymbolUpload
    {
        private readonly IDiagnosticLogger _logger;

        internal const string RelativeBuildOutputPathOld = "Temp/StagingArea/symbols";
        internal const string RelativeBuildOutputPathOldMono = "Temp/StagingArea/symbols";
        internal const string RelativeGradlePathOld = "Temp/gradleOut";
        internal const string RelativeBuildOutputPathNew = "Library/Bee/artifacts/Android";
        internal const string RelativeAndroidPathNew = "Library/Bee/Android";

        private readonly string _unityProjectPath;
        private readonly string _gradleProjectPath;

        private readonly SentryCliOptions? _cliOptions;
        internal string[] _symbolUploadPaths;

        private string _symbolUploadTask = @"
// Credentials and project settings information are stored in the sentry.properties file
gradle.taskGraph.whenReady {{
    gradle.taskGraph.allTasks[-1].doLast {{
        println 'Uploading symbols to Sentry'
        exec {{
            environment ""SENTRY_PROPERTIES"", ""./sentry.properties""
            executable ""{0}""
            args = [""upload-dif"", {1}]
        }}
    }}
}}";

        public DebugSymbolUpload(IDiagnosticLogger logger,
            SentryCliOptions? cliOptions,
            string unityProjectPath,
            string gradleProjectPath,
            bool isExporting = false,
            IApplication? application = null)
        {
            _logger = logger;

            _unityProjectPath = unityProjectPath;
            _gradleProjectPath = gradleProjectPath;

            _cliOptions = cliOptions;
            _symbolUploadPaths = GetSymbolUploadPaths(isExporting, application);
        }

        public void AppendUploadToGradleFile(string sentryCliPath)
        {
            var gradleFilePath = Path.Combine(_gradleProjectPath, "build.gradle");
            if (!File.Exists(gradleFilePath))
            {
                throw new FileNotFoundException("Failed to find 'build.gradle'.", _gradleProjectPath);
            }

            if (File.ReadAllText(gradleFilePath).Contains("sentry.properties"))
            {
                _logger.LogDebug("Symbol upload has already been added in a previous build.");
                return;
            }

            _logger.LogInfo("Appending debug symbols upload task to gradle file.");

            sentryCliPath = ConvertSlashes(sentryCliPath);
            if (!File.Exists(sentryCliPath))
            {
                throw new FileNotFoundException("Failed to find sentry-cli", sentryCliPath);
            }

            var uploadDifArguments = string.Empty;
            if (_cliOptions?.UploadSources ?? false)
            {
                uploadDifArguments += "\"--include-sources\",";
            }

            foreach (var symbolUploadPath in _symbolUploadPaths)
            {
                if (Directory.Exists(symbolUploadPath))
                {
                    uploadDifArguments += $"\"{ConvertSlashes(symbolUploadPath)}\",";
                }
                else
                {
                    throw new DirectoryNotFoundException($"Failed to find the symbols directory at {symbolUploadPath}");
                }
            }

            using var streamWriter = File.AppendText(gradleFilePath);
            streamWriter.Write(_symbolUploadTask, sentryCliPath, uploadDifArguments);
        }

        public void RemoveUploadFromGradleFile()
        {
            _logger.LogDebug("Removing the upload task from the gradle project.");

            var gradleFilePath = Path.Combine(_gradleProjectPath, "build.gradle");
            if (!File.Exists(gradleFilePath))
            {
                throw new FileNotFoundException($"Failed to find 'build.gradle'.", _gradleProjectPath);
            }

            var gradleBuildFile = File.ReadAllText(gradleFilePath);
            if (!gradleBuildFile.Contains("sentry.properties"))
            {
                _logger.LogDebug("No previous upload task found.");
                return;
            }

            // Replacing the paths with '.*' and escaping the task
            var uploadTaskFilter = string.Format(_symbolUploadTask, ".*", ".*");
            uploadTaskFilter = Regex.Replace(uploadTaskFilter, "\"", "\\\"");
            uploadTaskFilter = Regex.Replace(uploadTaskFilter, @"\[", "\\[");

            gradleBuildFile = Regex.Replace(gradleBuildFile, uploadTaskFilter, "");

            using var streamWriter = File.CreateText(gradleFilePath);
            streamWriter.Write(gradleBuildFile);
        }

        public void TryCopySymbolsToGradleProject(IApplication? application = null)
        {
            // The new building backend takes care of making the debug symbol files available within the exported project
            if (IsNewBuildingBackend(application))
            {
                _logger.LogDebug("New building backend. Skipping copying of debug symbols.");
                return;
            }

            _logger.LogInfo("Copying debug symbols to exported gradle project.");

            var buildOutputPath = Path.Combine(_unityProjectPath, RelativeBuildOutputPathOld);
            var targetRoot = Path.Combine(_gradleProjectPath, "symbols");

            foreach (var sourcePath in Directory.GetFiles(buildOutputPath, "*.so", SearchOption.AllDirectories))
            {
                var targetPath = sourcePath.Replace(buildOutputPath, targetRoot);
                _logger.LogDebug("Copying '{0}' to '{1}'", sourcePath, targetPath);

                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(targetPath)));
                FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
            }
        }

        internal string[] GetSymbolUploadPaths(bool isExporting, IApplication? application = null)
        {
            if (isExporting)
            {
                _logger.LogInfo("Exporting the project. Root for symbols upload: {0}", _gradleProjectPath);
                return new[] { _gradleProjectPath };
            }

            var paths = new List<string>();
            if (IsNewBuildingBackend(application))
            {
                _logger.LogInfo("Unity version 2021.2 or newer detected. Root for symbols upload: 'Library'.");
                if (!IsMono)
                {
                    paths.Add(Path.Combine(_unityProjectPath, RelativeBuildOutputPathNew));
                }
                paths.Add(Path.Combine(_unityProjectPath, RelativeAndroidPathNew));
            }
            else
            {
                _logger.LogInfo("Unity version 2021.1 or older detected. Root for symbols upload: 'Temp'.");
                if (!IsMono)
                {
                    paths.Add(Path.Combine(_unityProjectPath, RelativeBuildOutputPathOld));
                }
                paths.Add(Path.Combine(_unityProjectPath, RelativeGradlePathOld));
            }
            return paths.ToArray();
        }

        internal static bool IsNewBuildingBackend(IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            // Starting from 2021.2 Unity caches the build output inside 'Library' instead of 'Temp'
            var version = new Version(application.UnityVersion.Substring(0, 6)); // year.version
            if (version >= new Version("2021.2"))
            {
                return true;
            }

            return false;
        }

        // Gradle doesn't support backslashes on path (Windows) so converting to forward slashes
        internal static string ConvertSlashes(string path) => path.Replace(@"\", "/");

        private static bool IsMono => PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) == ScriptingImplementation.Mono2x;
    }
}
