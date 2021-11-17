using System;
using System.IO;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Android
{
    internal class DebugSymbolUpload
    {
        internal string _gradleProjectPath;
        internal string _exportedSymbolsPath = Path.Combine("unityLibrary", "src", "main", "symbols");
        internal string _symbolsPath;

        internal SentryCli _sentryCli;
        internal SentryCliOptions _sentryCliOptions;

        internal DebugSymbolUpload(string gradleProjectPath, string unityProjectPath, SentryCliOptions sentryCliOptions)
        {
            _gradleProjectPath = gradleProjectPath;
            _sentryCliOptions = sentryCliOptions;

            _sentryCli = new SentryCli();
            // TODO: create sentry.properties
            _symbolsPath = PrepareSymbols(Path.Combine(unityProjectPath, "Temp", "StagingArea", "symbols"));
        }

        internal string PrepareSymbols(string defaultSymbolsPath)
        {
            if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                // We can no longer trust the Unity temp directory to exist so we copy the symbols to the export directory.
                var symbolsTargetPath = Path.Combine(_gradleProjectPath, _exportedSymbolsPath);
                CopySymbols(defaultSymbolsPath, symbolsTargetPath);

                return symbolsTargetPath;
            }

            return defaultSymbolsPath;
        }

        internal void AppendToGradle(IDiagnosticLogger? logger = null)
        {
            try
            {
                var sentryCliPath = _sentryCli.GetSentryCliPath();
                var gradleFilePath = Path.Combine(_gradleProjectPath, "build.gradle");
                if (!File.Exists(gradleFilePath))
                {
                    throw new FileNotFoundException($"Could not find build.gradle at: {_gradleProjectPath}");
                }

                // There are two sets of symbols:
                // 1. The one specific to gradle (i.e. launcher)
                // 2. The ones Unity also provides via "Create Symbols.zip" that can be found in Temp/StagingArea/symbols/
                // We either get the path to the temp directory or if the project gets exported we copy the symbols to the output directory
                using var streamWriter = File.AppendText(gradleFilePath);
                streamWriter.Write($@"
gradle.taskGraph.whenReady {{
    gradle.taskGraph.allTasks[-1].doLast {{
        println 'Uploading symbols to Sentry'
        exec {{
            executable = ""{sentryCliPath}""
            args = [""--auth-token"", ""{_sentryCliOptions.Auth}"", ""upload-dif"", ""--org"", ""{_sentryCliOptions.Organization}"", ""--project"", ""{_sentryCliOptions.Project}"", ""./unityLibrary/src/main/jniLibs/""]
        }}
        exec {{
            executable = ""{sentryCliPath}""
            args = [""--auth-token"", ""{_sentryCliOptions.Auth}"", ""upload-dif"", ""--org"", ""{_sentryCliOptions.Organization}"", ""--project"", ""{_sentryCliOptions.Project}"", ""{_symbolsPath}""]
        }}
    }}
}}");
            }
            catch (Exception e)
            {
                logger?.LogError("Failed to add the automatic symbols upload to the gradle project", e);
            }
        }

        internal static void CopySymbols(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        internal void WriteToGradleFile(string filePath, string sentryCliPath, string symbolsPath)
        {

        }
    }
}
