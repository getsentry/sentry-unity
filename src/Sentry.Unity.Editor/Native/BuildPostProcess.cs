using System;
using System.IO;
using Sentry.Extensibility;
using System.Text.RegularExpressions;
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
            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            if (targetGroup is not BuildTargetGroup.Standalone)
            {
                return;
            }

            var options = SentryScriptableObject.LoadOptions()?.ToSentryUnityOptions(true);
            var logger = options?.DiagnosticLogger ?? new UnityLogger(options ?? new SentryUnityOptions());
            var isMono = PlayerSettings.GetScriptingBackend(targetGroup) == ScriptingImplementation.Mono2x;

            try
            {
                if (options?.IsValid() is not true)
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
                UploadDebugSymbols(logger, target, projectDir, executableName, options, isMono);
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

        private static void UploadDebugSymbols(IDiagnosticLogger logger, BuildTarget target, string projectDir, string executableName, SentryUnityOptions options, bool isMono)
        {
            var cliOptions = SentryScriptableObject.LoadCliOptions();
            if (cliOptions?.IsValid(logger) is not true)
            {
                if (options.Il2CppLineNumberSupportEnabled)
                {
                    logger.LogWarning("The IL2CPP line number support requires the debug symbol upload to be enabled.");
                }

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
                if (isMono)
                {
                    addPath("MonoBleedingEdge/EmbedRuntime");
                }
            }
            else if (target is BuildTarget.StandaloneLinux64)
            {
                addPath("GameAssembly.so");
                addPath("UnityPlayer.so");
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Linux/Sentry/libsentry.dbg.so"));
                if (isMono)
                {
                    addPath(Path.GetFileNameWithoutExtension(executableName) + "_Data/MonoBleedingEdge/x86_64");
                }
            }
            else if (target is BuildTarget.StandaloneOSX)
            {
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/Sentry/Sentry.dylib.dSYM"));
            }

            var cliArgs = "upload-dif --il2cpp-mapping ";
            if (cliOptions.UploadSources)
            {
                cliArgs += "--include-sources ";
            }
            cliArgs += paths;

            // Configure the process using the StartInfo properties.
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = SentryCli.SetupSentryCli(),
                    WorkingDirectory = projectDir,
                    Arguments = cliArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var propertiesFile = SentryCli.CreateSentryProperties(projectDir, cliOptions, options);
            try
            {
                process.StartInfo.EnvironmentVariables["SENTRY_PROPERTIES"] = propertiesFile;

                DataReceivedEventHandler logForwarder = (object sender, DataReceivedEventArgs e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        var msg = e.Data.Trim();
                        var msgLower = msg.ToLowerInvariant();
                        var level = SentryLevel.Info;
                        if (msgLower.StartsWith("error"))
                        {
                            level = SentryLevel.Error;
                        }
                        else if (msgLower.StartsWith("warn"))
                        {
                            level = SentryLevel.Warning;
                        }

                        // Remove the level and timestamp from the beginning of the message.
                        // INFO    2022-06-20 15:10:03.613794800 +02:00
                        msg = Regex.Replace(msg, "^[a-zA-Z]+ +[0-9\\-]{10} [0-9:]{8}\\.[0-9]+ \\+[0-9:]{5} +", "");
                        logger.Log(level, "sentry-cli: {0}", null, msg);
                    }
                };

                process.OutputDataReceived += logForwarder;
                process.ErrorDataReceived += logForwarder;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            finally
            {
                File.Delete(propertiesFile);
            }
        }
    }
}
