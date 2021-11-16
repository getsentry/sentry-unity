using System;
using System.IO;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Android
{
    public static class GradleDebugSymbolUpload
    {
        internal static string PackageName = "io.sentry.unity";
        internal static string PackageNameDev = "io.sentry.unity.dev";

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        [DllImport("libc", SetLastError = true)]
        private static extern int access(string pathname, int mode);

        internal static void AppendSymbolUploadToGradleProject(string basePath, SentryUnityOptions options)
        {
            var cliOptions = SentryCliOptions.LoadCliOptions();
            if (!cliOptions.UploadSymbols)
            {
                return;
            }

            if (EditorUserBuildSettings.development && !cliOptions.UploadDevelopmentSymbols)
            {
                return;
            }

            try
            {
                var gradleProjectPath = Directory.GetParent(basePath);
                var symbolsPath = GetSymbolsPath(Directory.GetParent(Application.dataPath).FullName, gradleProjectPath.FullName);
                var sentryCliPath = SetupSentryCli();

                // There are two sets of symbols:
                // 1. The one specific to gradle (i.e. launcher)
                // 2. The ones Unity also provides via "Create Symbols.zip" that can be found in Temp/StagingArea/symbols/
                // We either get the path to the temp directory or if the project gets exported we copy the symbols to the output directory
                using var streamWriter = File.AppendText(Path.Combine(gradleProjectPath.FullName, "build.gradle"));
                streamWriter.Write($@"
gradle.taskGraph.whenReady {{
    gradle.taskGraph.allTasks[-1].doLast {{
        println 'Uploading symbols to Sentry'
        exec {{
            executable = ""{sentryCliPath}""
            args = [""--auth-token"", ""{cliOptions.Auth}"", ""upload-dif"", ""--org"", ""{cliOptions.Organization}"", ""--project"", ""{cliOptions.Project}"", ""./unityLibrary/src/main/jniLibs/""]
        }}
        exec {{
            executable = ""{sentryCliPath}""
            args = [""--auth-token"", ""{cliOptions.Auth}"", ""upload-dif"", ""--org"", ""{cliOptions.Organization}"", ""--project"", ""{cliOptions.Project}"", ""{symbolsPath}""]
        }}
    }}
}}");
            }
            catch (Exception e)
            {
                options.DiagnosticLogger?.LogError("Failed to add the automatic symbols upload to the gradle project", e);
            }
        }

        internal static string GetSymbolsPath(string unityProjectPath, string gradleProjectPath)
        {
            // The default location for symbols
            var symbolsPath = Path.Combine(unityProjectPath, "Temp", "StagingArea", "symbols");

            if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                // When the gradle project gets exported we can no longer trust the Unity temp directory to exist so we
                // opt to copy the symbols to the export directory so sentry-cli can pick it up from there
                var targetPath = Path.Combine(gradleProjectPath, "unityLibrary", "src", "main", "symbols");
                CopySymbols(symbolsPath, targetPath);

                return targetPath;
            }

            return symbolsPath;
        }

        internal static void CopySymbols(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        internal static string SetupSentryCli()
        {
            var sentryCliPath = Path.GetFullPath(Path.Combine("Packages", GetPackageName(), "Editor", "sentry-cli", GetSentryCli()));
            if (!File.Exists(sentryCliPath))
            {
                throw new FileNotFoundException($"Could not find sentry-cli at path: {sentryCliPath}");
            }

            if (!SetPermissions(sentryCliPath))
            {
                // TODO: figure out what kind of exception is right here
                throw new UnauthorizedAccessException($"Failed to set execute permissions on {sentryCliPath}");
            }

            return sentryCliPath;
        }

        internal static string GetPackageName()
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

        internal static string GetSentryCli()
        {
            return Application.platform switch
            {
                RuntimePlatform.WindowsEditor => "sentry-cli-Windows-x86_64.exe ",
                RuntimePlatform.OSXEditor => "sentry-cli-Darwin-universal",
                RuntimePlatform.LinuxEditor => "sentry-cli-Linux-x86_64 ",
                _ => string.Empty
            };
        }

        internal static bool SetPermissions(string sentryCliPath)
        {
            // TODO: do I need this? Can I make it smaller? _0755 = (int)493

            // user permissions
            const int S_IRUSR = 0x100;
            const int S_IWUSR = 0x80;
            const int S_IXUSR = 0x40;

            // group permission
            const int S_IRGRP = 0x20;
            // const int S_IWGRP = 0x10;
            const int S_IXGRP = 0x8;

            // other permissions
            const int S_IROTH = 0x4;
            // const int S_IWOTH = 0x2;
            const int S_IXOTH = 0x1;

            const int _0755 =
                S_IRUSR | S_IXUSR | S_IWUSR
                | S_IRGRP | S_IXGRP
                | S_IROTH | S_IXOTH;

            if (access(Path.GetFullPath(sentryCliPath), (int)_0755) == 0)
            {
                return true;
            }

            return chmod(Path.GetFullPath(sentryCliPath), (int)_0755) == 0;
        }
    }
}
