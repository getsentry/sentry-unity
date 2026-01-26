using System;
using System.Collections.Generic;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Editor.ConfigurationWindow;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using System.Diagnostics;
using System.Linq;

namespace Sentry.Unity.Editor.Native;

public static class BuildPostProcess
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string executablePath)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
        if (targetGroup is not BuildTargetGroup.Standalone
            and not BuildTargetGroup.GameCoreXboxSeries
            and not BuildTargetGroup.PS5
            and not BuildTargetGroup.Switch)
        {
            return;
        }

        var cliOptions = SentryScriptableObject.LoadCliOptions();
        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
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

        // The executable path resolves to the following when pointing Unity into a `build/platform/` directory:
        // - Desktop: `./samples/unity-of-bugs/builds/windows/unityofbugs.exe`
        // - Xbox: `./samples/unity-of-bugs/builds/xsx/`
        // - PlayStation: `./samples/unity-of-bugs/builds/ps5/`
        // - Switch: `./samples/unity-of-bugs/builds/switch/unity-of-bugs.nspd_root` (file, not directory)
        var buildOutputDir = targetGroup switch
        {
            BuildTargetGroup.Standalone => Path.GetDirectoryName(executablePath),
            BuildTargetGroup.GameCoreXboxSeries => executablePath,
            BuildTargetGroup.PS5 => executablePath,
            BuildTargetGroup.Switch => Path.GetDirectoryName(executablePath),
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(buildOutputDir))
        {
            logger.LogError("Failed to find build output directory based on the executable path '{0}'." +
                            "\nSkipping adding crash-handler and uploading debug symbols.", executablePath);
            return;
        }

        var executableName = targetGroup switch
        {
            BuildTargetGroup.Standalone => Path.GetFileName(executablePath),
            // For Xbox/PS5, executablePath is the directory itself
            BuildTargetGroup.GameCoreXboxSeries => Path.GetFileName(executablePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            BuildTargetGroup.PS5 => Path.GetFileName(executablePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            _ => string.Empty
        };

        UploadDebugSymbols(logger, target, buildOutputDir, executableName, options, cliOptions, isMono);

        if (!IsEnabledForPlatform(target, options))
        {
            logger.LogDebug("Skipping adding the crash-handler. Native support for the current platform is disabled in the configuration.");
            return;
        }

        try
        {
            AddCrashHandler(logger, target, buildOutputDir);
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
        BuildTarget.PS5 => options.PlayStationNativeSupportEnabled,
        BuildTarget.Switch => options.SwitchNativeSupportEnabled,
        _ => false,
    };

    private static void AddCrashHandler(IDiagnosticLogger logger, BuildTarget target, string buildOutputDir)
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
                // No standalone crash handler for Xbox - comes with Breakpad
                return;
            case BuildTarget.PS5:
                // No standalone crash handler for PlayStation
                return;
            case BuildTarget.Switch:
                // No standalone crash handler for Switch - uses Nintendo's crash reporter
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

    internal static void AddPath(List<string> paths, string path, IDiagnosticLogger logger, bool required = false)
    {
        if (Directory.Exists(path) || File.Exists(path))
        {
            paths.Add(path);
            logger.LogDebug("Adding '{0}' to debug symbol upload", path);
        }
        else if (required)
        {
            logger.LogWarning("Required path not found for debug symbol upload: {0}", path);
        }
        else
        {
            logger.LogDebug("Optional path not found, skipping: {0}", path);
        }
    }

    private static void UploadDebugSymbols(IDiagnosticLogger logger, BuildTarget target, string buildOutputDir,
        string executableName, SentryUnityOptions options, SentryCliOptions? cliOptions, bool isMono)
    {
        var projectDir = Directory.GetParent(Application.dataPath)?.FullName ?? "";
        if (cliOptions?.IsValid(logger, EditorUserBuildSettings.development) is not true)
        {
            if (options.Il2CppLineNumberSupportEnabled)
            {
                logger.LogWarning("The IL2CPP line number support requires the debug symbol upload to be enabled.");
            }

            return;
        }

        // Warn if build output appears to be inside the Unity project directory
        if (Directory.Exists(Path.Combine(buildOutputDir, "Assets")) ||
            Directory.Exists(Path.Combine(buildOutputDir, "ProjectSettings")) ||
            Directory.Exists(Path.Combine(buildOutputDir, "Library")))
        {
            logger.LogWarning(
                "Build output directory '{0}' appears to be the Unity project directory or contain project folders. " +
                "This may cause issues with debug symbol upload. Consider using a dedicated build output folder outside the project.",
                buildOutputDir);
        }

        logger.LogInfo("Uploading debugging information using sentry-cli in {0}", buildOutputDir);

        var paths = new List<string>();

        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                // Core executables and libraries
                AddPath(paths, Path.Combine(buildOutputDir, executableName), logger, required: true);
                AddPath(paths, Path.Combine(buildOutputDir, "UnityPlayer.dll"), logger, required: true);

                // Sentry native SDK symbols from package
                AddPath(paths, Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Windows/Sentry/sentry.pdb"), logger);

                // Data - native plugins
                foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_Data"))
                {
                    AddPath(paths, dir, logger);
                }

                if (isMono)
                {
                    // Mono runtime
                    AddPath(paths, Path.Combine(buildOutputDir, "MonoBleedingEdge", "EmbedRuntime"), logger);
                    // Add all PDB files in build output root
                    foreach (var pdb in Directory.GetFiles(buildOutputDir, "*.pdb"))
                    {
                        AddPath(paths, pdb, logger);
                    }
                }
                else // IL2CPP
                {
                    AddPath(paths, Path.Combine(buildOutputDir, "GameAssembly.dll"), logger, required: true);
                    // IL2CPP output and Managed
                    foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BackUpThisFolder_*"))
                    {
                        AddPath(paths, dir, logger);
                    }
                }

                // Burst
                foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BurstDebugInformation_*"))
                {
                    AddPath(paths, dir, logger);
                }

                break;

            case BuildTarget.StandaloneLinux64:
                // Core executables and libraries
                AddPath(paths, Path.Combine(buildOutputDir, executableName), logger, required: true);
                AddPath(paths, Path.Combine(buildOutputDir, "UnityPlayer.so"), logger, required: true);

                // Sentry native SDK symbols from package
                AddPath(paths, Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Linux/Sentry/libsentry.dbg.so"), logger);

                // Data - native plugins
                foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_Data"))
                {
                    AddPath(paths, dir, logger);
                }

                if (!isMono) // IL2CPP
                {
                    AddPath(paths, Path.Combine(buildOutputDir, "GameAssembly.so"), logger, required: true);
                    // IL2CPP output and Managed
                    foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BackUpThisFolder_*"))
                    {
                        AddPath(paths, dir, logger);
                    }
                }

                // Burst
                foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BurstDebugInformation_*"))
                {
                    AddPath(paths, dir, logger);
                }

                break;

            case BuildTarget.StandaloneOSX:
                // App bundle
                AddPath(paths, Path.Combine(buildOutputDir, executableName), logger, required: true);

                // Sentry dSYM from package
                AddPath(paths, Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/Sentry/Sentry.dylib.dSYM"), logger);

                if (!isMono) // IL2CPP
                {
                    // IL2CPP output and Managed
                    foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BackUpThisFolder_*"))
                    {
                        AddPath(paths, dir, logger);
                    }

                }

                // Burst
                foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BurstDebugInformation_*"))
                {
                    AddPath(paths, dir, logger);
                }

                break;

            case BuildTarget.GameCoreXboxSeries:
            case BuildTarget.GameCoreXboxOne:
                // Xbox builds go to a dedicated directory, safe to scan entirely
                AddPath(paths, buildOutputDir, logger, required: true);
                // User-provided Sentry plugin
                AddPath(paths, Path.GetFullPath("Assets/Plugins/Sentry/"), logger);
                break;

            case BuildTarget.PS5:
                // PlayStation builds go to a dedicated directory, safe to scan entirely
                AddPath(paths, buildOutputDir, logger, required: true);
                // User-provided Sentry plugin
                AddPath(paths, Path.GetFullPath("Assets/Plugins/Sentry/"), logger);
                break;

            default:
                logger.LogError("Symbol upload for '{0}' is currently not supported.", target);
                return;
        }

        // Possible duplicate but check for the .pdb files that Unity stores for script assemblies in `./Temp/ManagedSymbols/`.
        var managedSymbolsDirectory = Path.Combine(projectDir, "Temp", "ManagedSymbols");
        AddPath(paths, managedSymbolsDirectory, logger);

        if (paths.Count == 0)
        {
            logger.LogWarning("No debug symbol paths found to upload.");
            return;
        }

        // Build CLI arguments
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
            cliArgs += "--allow-failure ";
        }

        cliArgs = paths.Aggregate(cliArgs, (current, path) => current + $"\"{path}\" ");

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
