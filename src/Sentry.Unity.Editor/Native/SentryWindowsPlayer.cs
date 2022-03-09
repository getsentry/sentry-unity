using System;
using System.Diagnostics;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Sentry.Unity.Editor.Native
{
    internal static class SentryWindowsPlayer
    {
        internal static readonly ProcessStartInfo StartInfo = new()
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        internal static void AddNativeOptions(SentryUnityOptions options)
        {
        }

        internal static void AddSentryToMain(SentryUnityOptions options)
        {
        }

        internal static string LocateWindowsPlayerSource(IEditorApplication? editorApplication = null)
        {
            editorApplication ??= EditorApplicationAdapter.Instance;

            var playerProjectPath = Path.Combine(editorApplication.ApplicationContentsPath, "PlaybackEngines", "windowsstandalonesupport", "source", "windowsplayer");
            if (!Directory.Exists(playerProjectPath))
            {
                throw new DirectoryNotFoundException($"Failed to locate the WindowsPlayer source at {playerProjectPath}.");
            }

            return playerProjectPath;
        }

        internal static void CreateWindowsPlayerProject(string windowsPlayerSource, string windowsPlayerTarget, IDiagnosticLogger? logger)
        {
            EditorFileIO.CopyDirectory(windowsPlayerSource, windowsPlayerTarget, logger);

            // TODO: Does the .props file have to look like that?
            using var props = File.CreateText(Path.Combine(windowsPlayerTarget, "UnityCommon.props"));
            props.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>");
        }

        internal static string LocateMSBuild(string vsWherePath, IDiagnosticLogger? logger)
        {
            StartInfo.FileName = vsWherePath;
            StartInfo.Arguments = "-latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe";

            var vsWhereOutput = "";
            var process = new Process {StartInfo = StartInfo};
            process.OutputDataReceived += (sender, args) => vsWhereOutput += args.Data;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            logger?.LogDebug("VSWhere returned with: {0}", vsWhereOutput);

            if (!File.Exists(vsWhereOutput))
            {
                throw new FileNotFoundException($"Failed to locate 'msbuild'. VSWhere returned {vsWhereOutput}");
            }

            return vsWhereOutput;
        }

        internal static string LocateVSWhere(IDiagnosticLogger? logger)
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            var directories = Directory.GetDirectories(Path.Combine(projectPath, "Library", "PackageCache"), "com.unity.ide.visualstudio@*");
            if (directories is null || directories.Length < 1)
            {
                throw new Exception("Failed lo locate the 'com.unity.ide.visualstudio' package.");
            }

            // TODO: Not sure if there can be more than one version of a package in the Library/PackageCache and if it even matters
            var vsPackagePath = directories[0];
            logger?.LogDebug("Located 'com.unity.ide.visualstudio' package at {0}", vsPackagePath);

            var vsWherePath = Path.Combine(vsPackagePath, "Editor", "VSWhere", "vswhere.exe");
            if (!File.Exists(vsWherePath))
            {
                throw new FileNotFoundException($"Failed to find 'vswhere.exe' at '{vsWherePath}'");
            }

            return vsWherePath;
        }

        public static void Build(SentryUnityOptions options, string executablePath)
        {
            var vsWherePath = LocateVSWhere(options.DiagnosticLogger);
            var msBuildPath = LocateMSBuild(vsWherePath, options.DiagnosticLogger);

            var playerSource = LocateWindowsPlayerSource();
            var playerTarget = FileUtil.GetUniqueTempPathInProject();

            CreateWindowsPlayerProject(playerSource, playerTarget, options.DiagnosticLogger);

            AddNativeOptions(options);
            AddSentryToMain(options);

            StartInfo.FileName = msBuildPath;
            StartInfo.Arguments = playerTarget;

            var outputData = "";
            var errorData = "";
            var process = new Process {StartInfo = StartInfo};
            process.OutputDataReceived += (sender, args) => outputData += args.Data;
            process.ErrorDataReceived += (sender, args) => errorData += args.Data;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (outputData.Contains("Build succeeded"))
            {
                options.DiagnosticLogger?.LogDebug("Succeeded building the PlaybackEngine");
            }
            else if (outputData.Contains("Build failed"))
            {
                throw new Exception($"Failed to build the PlaybackEngine: \n {outputData}");
            }

            var logFile = Path.Combine(playerTarget, "build.log");
            File.WriteAllText(logFile, outputData);
            File.AppendAllText(logFile, errorData);

            // TODO: Overwrite the executable with the build output
        }
    }
}
