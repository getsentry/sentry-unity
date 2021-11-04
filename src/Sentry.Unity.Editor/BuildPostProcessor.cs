using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Sentry.Unity.Editor.Android
{
    public static class BuildPostProcessor
    {
        private static string PackageName = "io.sentry.unity";
        private static string PackageNameDev = "io.sentry.unity.dev";

        private const string testPathToBuiltProject = @"/Users/bitfox/_Workspace/Unity/samples/Builds/test.apk";

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.Android)
            {
                return;
            }

            UploadSymbols(pathToBuiltProject);
        }

        [MenuItem("Tools/SentryUploadSymbols")]
        public static void PostProcess() => UploadSymbols(testPathToBuiltProject);

        [MenuItem("Tools/Test")]
        public static void Test()
        {
            Debug.Log(GetSentryCliPath());
        }

        public static void UploadSymbols(string pathToBuiltProject)
        {
            if (!EditorUserBuildSettings.androidCreateSymbolsZip)
            {
                Debug.Log("no symbols.zip created. skipping upload");
                return;
            }

            var sentryCliOptions = AssetDatabase.LoadAssetAtPath("Assets/Plugins/Sentry/SentryCliOptions.asset",
                typeof(SentryCliOptions)) as SentryCliOptions;
            var sentryCliPath = GetSentryCliPath();

            if (sentryCliOptions is null || !sentryCliOptions.UploadSymbols || sentryCliPath is null)
            {
                Debug.Log("sentry-cli options said no");
                return;
            }

            if (EditorUserBuildSettings.development && !sentryCliOptions.UploadDevelopmentSymbols)
            {
                Debug.Log("not uploading development symbols");
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
            var sentryCli = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                sentryCli = "sentry-cli-Darwin-universal";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sentryCli = "sentry-cli-Windows-x86_64.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                sentryCli = "sentry-cli-Linux-x86_64";
            }

            var sentryCliPath = Path.GetFullPath(Path.Combine("Packages", GetPackageName(), "Editor/sentry-cli/", sentryCli));
            if (!File.Exists(sentryCliPath))
            {
                Debug.LogWarning("failed to find sentry-cli");
                return null;
            }

            return sentryCliPath;
        }

        private static string GetPackageName()
        {
            var packagePath = Path.Combine("Packages", PackageName);
            if (Directory.Exists(Path.Combine(packagePath)))
            {
                return PackageName;
            }

            packagePath = Path.Combine("Packages", PackageNameDev);
            if (Directory.Exists(Path.Combine(packagePath)))
            {
                return PackageNameDev;
            }

            throw new FileNotFoundException("Failed to locate the Sentry package");
        }
    }
}
