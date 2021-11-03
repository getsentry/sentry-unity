using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Sentry.Unity.Editor.Android
{
    public static class AndroidBuildPostProcessor
    {
        private const string pathToBuiltProject = @"/Users/bitfox/_Workspace/Unity/samples/Builds/test.apk";

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) => UploadSymbols(pathToBuiltProject);

        [MenuItem("Tools/SentryUploadSymbols")]
        public static void PostProcess() => UploadSymbols(pathToBuiltProject);

        public static void UploadSymbols(string pathToBuiltProject)
        {
            var sentryCliOptions = AssetDatabase.LoadAssetAtPath("Assets/Plugins/Sentry/SentryCliOptions.asset",
                typeof(SentryCliOptions)) as SentryCliOptions;
            var sentryCliPath = GetSentryCliPath();

            if (sentryCliOptions is null || !sentryCliOptions.UploadSymbols || sentryCliPath is null)
            {
                return;
            }

            var builtProjectName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            var filesInDirectory = Directory.GetFiles(Path.GetDirectoryName(pathToBuiltProject)).ToList();

            var symbolsFile = filesInDirectory.Find(f => f.EndsWith("symbols.zip") && f.Contains($"{builtProjectName}-{Application.version}"));
            if (symbolsFile is null)
            {
                Debug.LogWarning("failed to locate symbols file");
                return;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = sentryCliPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                Arguments = $"--auth-token {sentryCliOptions.Auth} upload-dif --org {sentryCliOptions.Organization} --project {sentryCliOptions.Project} {symbolsFile}"
            };

            var process = Process.Start(processInfo);
            var output = process.StandardOutput.ReadToEnd();
            Debug.Log(output);

            process.WaitForExit();
        }

        public static string? GetSentryCliPath()
        {
            // Todo: return path depending on platform

            var sentryCliPath = Path.GetFullPath("Packages/io.sentry.unity.dev/Editor/sentry-cli/sentry-cli-Darwin-x86_64");
            if (!File.Exists(sentryCliPath))
            {
                Debug.LogWarning("failed to find sentry-cli");
                return null;
            }

            return sentryCliPath;
        }
    }
}
