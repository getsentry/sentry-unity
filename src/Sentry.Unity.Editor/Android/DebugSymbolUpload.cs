using System.IO;
using UnityEditor;

namespace Sentry.Unity.Editor.Android
{
    internal static class DebugSymbolUpload
    {
        internal static readonly string _gradleExportedSymbolsPath = Path.Combine("unityLibrary", "src", "main", "symbols");

        public static string GetSymbolsPath(string unityProjectPath, string gradleProjectPath, bool isProjectExporting)
        {
            var symbolsPath = Path.Combine(unityProjectPath, "Temp", "StagingArea", "symbols");
            if (isProjectExporting)
            {
                // We can no longer trust the Unity temp directory to exist so we copy the symbols to the export directory.
                var copiedSymbolsPath = Path.Combine(gradleProjectPath, _gradleExportedSymbolsPath);
                CopySymbols(symbolsPath, copiedSymbolsPath);

                return copiedSymbolsPath;
            }

            return symbolsPath;
        }

        public static void AppendUploadToGradleFile(string sentryCliPath, string gradleProjectPath, string symbolsDirectoryPath)
        {
            if (!File.Exists(sentryCliPath))
            {
                throw new FileNotFoundException($"Failed to find sentry-cli at {sentryCliPath}");
            }

            var gradleFilePath = Path.Combine(gradleProjectPath, "build.gradle");
            if (!File.Exists(gradleFilePath))
            {
                throw new FileNotFoundException($"Failed to find 'build.gradle' at: {gradleProjectPath}");
            }

            if (!Directory.Exists(symbolsDirectoryPath))
            {
                throw new DirectoryNotFoundException($"Failed to find the symbols directory at {symbolsDirectoryPath}");
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
            args = [""upload-dif"", ""./unityLibrary/src/main/jniLibs/""]
        }}
        exec {{
            executable = ""{sentryCliPath}""
            args = [""upload-dif"", ""{symbolsDirectoryPath}""]
        }}
    }}
}}");
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
    }
}
