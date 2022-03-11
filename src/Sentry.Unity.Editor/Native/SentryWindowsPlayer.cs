using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Sentry.Extensibility;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Sentry.Unity.Editor.Native
{
    internal class SentryWindowsPlayer
    {
        private readonly string _projectPath;
        private readonly IDiagnosticLogger? _logger;

        internal SentryWindowsPlayer(string projectPath, IDiagnosticLogger? logger)
        {
            _projectPath = projectPath;
            _logger = logger;
        }

        public static SentryWindowsPlayer Create(IDiagnosticLogger? logger)
        {
            var playerTarget = FileUtil.GetUniqueTempPathInProject();
            CreateWindowsPlayerProject(LocateWindowsPlayerSource(), playerTarget, logger);

            return new SentryWindowsPlayer(playerTarget, logger);
        }

        public void AddNativeOptions()
        {
            _logger?.LogDebug("Adding Native Options.");
        }

        public void AddSentryToMain()
        {
            _logger?.LogDebug("Adding Sentry to main.");
        }

        public void Build(string msBuildPath, string executablePath)
        {
            _logger?.LogDebug("Building Sentry Windows Player.");

            if (!File.Exists(msBuildPath))
            {
                throw new FileNotFoundException($"Failed to find MSBuild at '{msBuildPath}'.");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = msBuildPath,
                    Arguments = _projectPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var output = new StringBuilder();
            var errorOutput = new StringBuilder();
            process.OutputDataReceived += (sender, args) => output.Append(args.Data);
            process.ErrorDataReceived += (sender, args) => errorOutput.Append(args.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var logFile = Path.Combine(_projectPath, "build.log");
            File.WriteAllText(logFile, output.ToString());
            File.AppendAllText(logFile, errorOutput.ToString());

            if (process.ExitCode == 0)
            {
                _logger?.LogDebug("MSBuild succeeded building the Windows Player");
            }
            else
            {
                throw new Exception($"MSBuild failed to build the Windows Player. Look at '{logFile}' for more information");
            }

            // TODO: Overwrite the executable with the build output
            _logger?.LogDebug("Copying player to build output directory.");
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

        internal static void CreateWindowsPlayerProject(string projectSource, string projectTarget, IDiagnosticLogger? logger)
        {
            logger?.LogDebug("Creating player project from source from '{0}' at '{1}'", projectSource, projectTarget);

            EditorFileIO.CopyDirectory(projectSource, projectTarget, logger);

            // TODO: Does the .props file have to look like that?
            // The 'UnityCommon.props' is missing from the provided source code
            using var props = File.CreateText(Path.Combine(projectTarget, "UnityCommon.props"));
            props.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>");
        }
    }
}
