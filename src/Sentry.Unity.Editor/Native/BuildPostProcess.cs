using System;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Editor.ConfigurationWindow;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using System.Diagnostics;

namespace Sentry.Unity.Editor.Native;

public static class BuildPostProcess
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string executablePath)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
        // TODO: Is Xbox standalone?
        // if (targetGroup is not BuildTargetGroup.Standalone)
        // {
        //     return;
        // }

        var (options, cliOptions) = SentryScriptableObject.ConfiguredBuildTimeOptions();
        var logger = options?.DiagnosticLogger ?? new UnityLogger(options ?? new SentryUnityOptions());

        if (options is null)
        {
            logger.LogWarning("Native support disabled because Sentry has not been configured. " +
                              "You can do that through the editor: {0}", SentryWindow.EditorMenuPath);
            return;
        }

        if (!options.IsValid())
        {
            logger.LogDebug("Skipping native post build process.");
            return;
        }

#pragma warning disable CS0618
        var isMono = PlayerSettings.GetScriptingBackend(targetGroup) == ScriptingImplementation.Mono2x;
#pragma warning restore CS0618

        var executableName = Path.GetFileName(executablePath);
        var buildOutputDir = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrEmpty(buildOutputDir))
        {
            logger.LogError("Failed to find build output directory based on the executable path '{0}'." +
                            "\nSkipping adding crash-handler and uploading debug symbols.", executablePath);
            return;
        }

        UploadDebugSymbols(logger, target, buildOutputDir, executableName, options, cliOptions, isMono);

        if (!IsEnabledForPlatform(target, options))
        {
            logger.LogDebug("Skipping adding the crash-handler. Native support for the current platform is disabled in the configuration.");
            return;
        }

        try
        {
            AddCrashHandler(logger, target, buildOutputDir, executableName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add the crash-handler to the built application.");
            throw new BuildFailedException("Sentry Native BuildPostProcess failed");
        }
    }

    private static bool IsEnabledForPlatform(BuildTarget target, SentryUnityOptions options) => target switch
    {
        BuildTarget.StandaloneWindows => options.WindowsNativeSupportEnabled,
        BuildTarget.StandaloneWindows64 => options.WindowsNativeSupportEnabled,
        BuildTarget.StandaloneOSX => options.MacosNativeSupportEnabled,
        BuildTarget.StandaloneLinux64 => options.LinuxNativeSupportEnabled,
        (BuildTarget)42 => true,
        _ => false,
    };

    private static void AddCrashHandler(IDiagnosticLogger logger, BuildTarget target, string buildOutputDir, string executableName)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                logger.LogDebug("Adding crashpad.");
                CopyHandler(logger, buildOutputDir, Path.Combine("Windows", "Sentry", "crashpad_handler.exe"));
                CopyHandler(logger, buildOutputDir, Path.Combine("Windows", "Sentry", "crashpad_wer.dll"));
                break;
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneOSX:
                // No standalone crash handler for Linux/macOS - uses built-in handlers.
                return;
            case (BuildTarget)42:
                logger.LogDebug("I know it's Xbox but I don't know what do to with it (yet).");
                break;
            default:
                throw new ArgumentException($"Unsupported build target: {target}");
        }
    }

    private static void CopyHandler(IDiagnosticLogger logger, string buildOutputDir, string handlerPath)
    {
        var fullHandlerPath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Plugins", handlerPath));
        var targetHandlerPath = Path.Combine(buildOutputDir, Path.GetFileName(fullHandlerPath));
        logger.LogInfo("Copying handler '{0}' to {1}", Path.GetFileName(fullHandlerPath), targetHandlerPath);
        File.Copy(fullHandlerPath, targetHandlerPath, true);
    }

    private static void UploadDebugSymbols(IDiagnosticLogger logger, BuildTarget target, string buildOutputDir, string executableName, SentryUnityOptions options, SentryCliOptions? cliOptions, bool isMono)
    {
        var projectDir = Directory.GetParent(Application.dataPath).FullName;
        if (cliOptions?.IsValid(logger, EditorUserBuildSettings.development) is not true)
        {
            if (options.Il2CppLineNumberSupportEnabled)
            {
                logger.LogWarning("The IL2CPP line number support requires the debug symbol upload to be enabled.");
            }

            return;
        }

        logger.LogInfo("Uploading debugging information using sentry-cli in {0}", buildOutputDir);

        var paths = "";
        Func<string, bool> addPath = (string name) =>
        {
            var fullPath = Path.Combine(buildOutputDir, name);
            if (fullPath.Contains("*") || Directory.Exists(fullPath) || File.Exists(fullPath))
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

        Action<string, string[]> addFilesMatching = (string directory, string[] includePatterns) =>
        {
            Matcher matcher = new();
            matcher.AddIncludePatterns(includePatterns);
            foreach (string file in matcher.GetResultsInFullPath(directory))
            {
                addPath(file);
            }
        };

        addPath(executableName);

        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                addPath("UnityPlayer.dll");
                addPath(Path.GetFileNameWithoutExtension(executableName) + "_Data/Plugins/x86_64/sentry.dll");
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Windows/Sentry/sentry.pdb"));

                if (isMono)
                {
                    addPath("MonoBleedingEdge/EmbedRuntime");
                    addFilesMatching(buildOutputDir, new[] { "*.pdb" });

                    // Unity stores the .pdb files in './Library/ScriptAssemblies/' and starting with 2020 in
                    // './Temp/ManagedSymbols/'. We want the one in 'Temp/ManagedSymbols/' specifically.
                    var managedSymbolsDirectory = $"{projectDir}/Temp/ManagedSymbols";
                    if (Directory.Exists(managedSymbolsDirectory))
                    {
                        addFilesMatching(managedSymbolsDirectory, new[] { "*.pdb" });
                    }
                }
                else // IL2CPP
                {
                    addPath(Path.GetFileNameWithoutExtension(executableName) + "_BackUpThisFolder_ButDontShipItWithYourGame");
                    addPath("GameAssembly.dll");
                }
                break;
            case BuildTarget.StandaloneLinux64:
                addPath("GameAssembly.so");
                addPath("UnityPlayer.so");
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Linux/Sentry/libsentry.dbg.so"));

                if (isMono)
                {
                    addPath(Path.GetFileNameWithoutExtension(executableName) + "_Data/MonoBleedingEdge/x86_64");
                    addFilesMatching(buildOutputDir, new[] { "*.debug" });

                    var managedSymbolsDirectory = $"{projectDir}/Temp/ManagedSymbols";
                    if (Directory.Exists(managedSymbolsDirectory))
                    {
                        addFilesMatching(managedSymbolsDirectory, new[] { "*.pdb" });
                    }
                }
                else // IL2CPP
                {
                    addPath(Path.GetFileNameWithoutExtension(executableName) + "_BackUpThisFolder_ButDontShipItWithYourGame");
                }
                break;
            case BuildTarget.StandaloneOSX:
                addPath(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/Sentry/Sentry.dylib.dSYM"));

                if (isMono)
                {
                    addFilesMatching(buildOutputDir, new[] { "*.pdb" });

                    // Unity stores the .pdb files in './Library/ScriptAssemblies/' and starting with 2020 in
                    // './Temp/ManagedSymbols/'. We want the one in 'Temp/ManagedSymbols/' specifically.
                    var managedSymbolsDirectory = $"{projectDir}/Temp/ManagedSymbols";
                    if (Directory.Exists(managedSymbolsDirectory))
                    {
                        addFilesMatching(managedSymbolsDirectory, new[] { "*.pdb" });
                    }
                }
                else // IL2CPP
                {
                    addPath(Path.GetFileNameWithoutExtension(executableName) + "_BackUpThisFolder_ButDontShipItWithYourGame");
                }
                break;
            case (BuildTarget)42:
                logger.LogDebug("This is where I would attempt to upload stuff for Xbox to Sentry as well.");
                break;
            default:
                logger.LogError($"Symbol upload for '{target}' is currently not supported.");
                break;
        }

        var cliArgs = "debug-files upload ";
        if (!isMono)
        {
            cliArgs += "--il2cpp-mapping ";
        }
        if (cliOptions.UploadSources)
        {
            cliArgs += "--include-sources ";
        }
        if (cliOptions.IgnoreCliErrors)
        {
            cliArgs += "--allow-failure";
        }
        cliArgs += paths;

        // Configure the process using the StartInfo properties.
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = SentryCli.SetupSentryCli(),
                WorkingDirectory = buildOutputDir,
                Arguments = cliArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var propertiesFile = SentryCli.CreateSentryProperties(buildOutputDir, cliOptions, options);
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
