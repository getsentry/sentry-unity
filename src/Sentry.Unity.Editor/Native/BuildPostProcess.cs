using System;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using System.Diagnostics;

namespace Sentry.Unity.Editor.Native
{
    public static class BuildPostProcess
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string executablePath)
        {
            if (EditorUserBuildSettings.selectedBuildTargetGroup is not BuildTargetGroup.Standalone)
            {
                return;
            }

            var options = SentryScriptableObject
                .Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath())
                ?.ToSentryUnityOptions(BuildPipeline.isBuildingPlayer);
            var logger = options?.DiagnosticLogger ?? new UnityLogger(options ?? new SentryUnityOptions());

            try
            {
                if (PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup) != ScriptingImplementation.IL2CPP)
                {
                    logger.LogWarning("Failed to enable Native support - only available with IL2CPP scripting backend.");
                    return;
                }

                if (options is null)
                {
                    logger.LogWarning("Native support disabled. " +
                                      "Sentry has not been configured. You can do that through the editor: Tools -> Sentry");
                    return;
                }

                if (!options.IsValid())
                {
                    logger.LogDebug("Native support disabled.");
                    return;
                }

                if (!IsEnabledForPlatform(target, options))
                {
                    logger.LogDebug("Native support for the current platform is disabled in the configuration.");
                    return;
                }

                logger.LogDebug("Adding native support.");

                var projectDir = Path.GetDirectoryName(executablePath);
                var executableName = Path.GetFileName(executablePath);
                AddCrashHandler(logger, target, projectDir, executableName);
                UploadDebugSymbols(logger, target, projectDir, executableName);
            }
            catch (Exception e)
            {
                logger.LogError("Failed to add the Sentry native integration to the built application", e);
                throw new BuildFailedException("Sentry Native BuildPostProcess failed");
            }
        }

        private static bool IsEnabledForPlatform(BuildTarget target, SentryUnityOptions options) => target switch
        {
            BuildTarget.StandaloneWindows64 => options.WindowsNativeSupportEnabled,
            BuildTarget.StandaloneOSX => options.MacosNativeSupportEnabled,
            BuildTarget.StandaloneLinux64 => options.LinuxNativeSupportEnabled,
            _ => false,
        };

        private static void AddCrashHandler(IDiagnosticLogger logger, BuildTarget target, string projectDir, string executableName)
        {
            string crashpadPath;
            if (target is BuildTarget.StandaloneWindows64)
            {
                crashpadPath = Path.Combine("Windows", "Sentry", "crashpad_handler.exe");
            }
            else if (target is BuildTarget.StandaloneLinux64)
            {
                // No standalone crash handler for Linux - uses built-in breakpad.
                return;
            }
            else if (target is BuildTarget.StandaloneOSX)
            {
                // No standalone crash handler for macOS - uses built-in handler in sentry-cocoa.
                return;
            }
            else
            {
                throw new ArgumentException($"Unsupported build target: {target}");
            }

            crashpadPath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", crashpadPath));
            var targetPath = Path.Combine(projectDir, Path.GetFileName(crashpadPath));
            logger.LogInfo("Copying the native crash handler '{0}' to {1}", Path.GetFileName(crashpadPath), targetPath);
            File.Copy(crashpadPath, targetPath, true);
        }

        private static void UploadDebugSymbols(IDiagnosticLogger logger, BuildTarget target, string projectDir, string executableName)
        {
            var cliOptions = SentryScriptableObject.CreateOrLoad<SentryCliOptions>(SentryCliOptions.GetConfigPath());
            if (!cliOptions.IsValid(logger))
            {
                return;
            }

            logger.LogInfo("Uploading debugging information using sentry-cli in {0}", projectDir);

            var paths = "";
            Func<string, bool> addPath = (string name) =>
            {
                var fullPath = Path.Combine(projectDir, name);
                if (Directory.Exists(fullPath) || File.Exists(fullPath))
                {
                    paths += $" \"{name}\"";
                    logger.LogDebug($"Adding '{name}' to the debug-info upload");
                    return true;
                }
                else
                {
                    logger.LogWarning($"Couldn't find '{name}' - debug symbol upload will be incomplete");
                    return false;
                }
            };

            addPath(executableName);
            addPath(Path.GetFileNameWithoutExtension(executableName) + "_BackUpThisFolder_ButDontShipItWithYourGame");
            if (target is BuildTarget.StandaloneWindows64)
            {
                addPath("GameAssembly.dll");
                addPath("UnityPlayer.dll");
                addPath(Path.GetFileNameWithoutExtension(executableName) + "_Data/Plugins/x86_64/sentry.dll");
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Windows/Sentry/sentry.pdb"));
            }
            else if (target is BuildTarget.StandaloneLinux64)
            {
                addPath("GameAssembly.so");
                addPath("UnityPlayer.so");
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Linux/Sentry/libsentry.dbg.so"));
            }
            else if (target is BuildTarget.StandaloneOSX)
            {
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/Sentry/Sentry.dylib.dSYM"));
            }

            // Configure the process using the StartInfo properties.
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = SentryCli.SetupSentryCli(),
                    WorkingDirectory = projectDir,
                    Arguments = "upload-dif " + paths,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            if (!string.IsNullOrEmpty(cliOptions.UrlOverride))
            {
                process.StartInfo.EnvironmentVariables["SENTRY_URL"] = cliOptions.UrlOverride;
            }

            process.StartInfo.EnvironmentVariables["SENTRY_ORG"] = cliOptions.Organization;
            process.StartInfo.EnvironmentVariables["SENTRY_PROJECT"] = cliOptions.Project;
            process.StartInfo.EnvironmentVariables["SENTRY_AUTH_TOKEN"] = cliOptions.Auth;
            process.OutputDataReceived += (sender, args) => logger.LogDebug($"sentry-cli: {args.Data.ToString()}");
            process.ErrorDataReceived += (sender, args) => logger.LogError($"sentry-cli: {args.Data.ToString()}");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }
}
