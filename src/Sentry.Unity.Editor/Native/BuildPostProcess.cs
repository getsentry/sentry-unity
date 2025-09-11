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
        if (targetGroup is not BuildTargetGroup.Standalone and not BuildTargetGroup.GameCoreXboxSeries)
        {
            return;
        }

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
        BuildTarget.GameCoreXboxSeries or BuildTarget.GameCoreXboxOne => options.XboxNativeSupportEnabled,
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
            case BuildTarget.GameCoreXboxSeries:
            case BuildTarget.GameCoreXboxOne:
                // TODO: Figure out if we need to ship with a crash handler
                return;
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

        // Setting the `buildOutputDir` as the root for debug symbol upload. This will make sure we pick up
        // `BurstDebugInformation` and`_DoNotShip` as well
        var paths = $" \"{buildOutputDir}\"";

        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                var windowsSentryPdb = Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Windows/Sentry/sentry.pdb");
                if (File.Exists(windowsSentryPdb))
                {
                    paths += $" \"{windowsSentryPdb}\"";
                }
                break;
            case BuildTarget.StandaloneLinux64:
                var linuxSentryDbg = Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Linux/Sentry/libsentry.dbg.so");
                if (File.Exists(linuxSentryDbg))
                {
                    paths += $" \"{linuxSentryDbg}\"";
                }
                break;
            case BuildTarget.StandaloneOSX:
                var macOSSentryDsym = Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/Sentry/Sentry.dylib.dSYM");
                if (Directory.Exists(macOSSentryDsym))
                {
                    paths += $" \"{macOSSentryDsym}\"";
                }
                break;
            default:
                logger.LogError($"Symbol upload for '{target}' is currently not supported.");
                return;
        }

        // Unity stores the .pdb files for script assemblies in `./Temp/ManagedSymbols/`
        var managedSymbolsDirectory = $"{projectDir}/Temp/ManagedSymbols";
        if (Directory.Exists(managedSymbolsDirectory))
        {
            paths += $" \"{managedSymbolsDirectory}\"";
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
                    var level = SentryLevel.Debug;
                    if (msgLower.StartsWith("error"))
                    {
                        level = SentryLevel.Error;
                    }
                    else if (msgLower.StartsWith("warn"))
                    {
                        level = SentryLevel.Warning;
                    }
                    else if (msgLower.StartsWith("info"))
                    {
                        level = SentryLevel.Info;
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
