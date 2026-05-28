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

        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        var isMono = PlayerSettings.GetScriptingBackend(namedBuildTarget) == ScriptingImplementation.Mono2x;

        // The executable path resolves to the following when pointing Unity into a `build/platform/` directory:
        // - Desktop: `./samples/unity-of-bugs/builds/windows/unityofbugs.exe`
        // - Xbox: `./samples/unity-of-bugs/builds/xsx/`
        // - PlayStation: `./samples/unity-of-bugs/builds/ps5/`
        // - Switch: `./samples/unity-of-bugs/builds/switch/unity-of-bugs.nspd_root`
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

        // Setup the actual plugin and crash handler in the build ouput directory
        try
        {
            if (target == BuildTarget.StandaloneOSX)
            {
                // Since the backend can change between iterative builds we need to clean up after ourselves
                CleanupStaleMacOSArtifacts(logger, executablePath);
            }

            foreach (var artifact in GetNativePluginArtifact(target, options, executablePath, buildOutputDir))
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(artifact.Destination));
                logger.LogDebug("Copying '{0}' to '{1}'", artifact.Source, artifact.Destination);
                File.Copy(artifact.Source, artifact.Destination, overwrite: true);
                if (artifact.MarkExecutable)
                {
                    SentryCli.SetExecutePermission(artifact.Destination);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to copy Sentry runtime artifacts into the built application.");
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

    private readonly struct NativePluginArtifact(string source, string destination, bool isExecutable = false)
    {
        public readonly string Source = source;
        public readonly string Destination = destination;
        public readonly bool MarkExecutable = isExecutable;
    }

    private static IEnumerable<NativePluginArtifact> GetNativePluginArtifact(
        BuildTarget target, SentryUnityOptions options, string executablePath, string buildOutputDir)
    {
        var pluginsPath = Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins");

        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                // Crashpad must sit next to the player .exe so sentry-native can spawn it on crash
                yield return new NativePluginArtifact(
                    Path.Combine(pluginsPath, "Windows", "Sentry", "crashpad_handler.exe"),
                    Path.Combine(buildOutputDir, "crashpad_handler.exe"));
                yield return new NativePluginArtifact(
                    Path.Combine(pluginsPath, "Windows", "Sentry", "crashpad_wer.dll"),
                    Path.Combine(buildOutputDir, "crashpad_wer.dll"));
                break;

            case BuildTarget.StandaloneOSX:
                var backendSourcePath = options.Experimental.MacosBackend == MacosBackend.Native
                    ? Path.Combine(pluginsPath, "macOS", "SentryNative~")
                    : Path.Combine(pluginsPath, "macOS", "Sentry~");
                var contents = Path.Combine(executablePath, "Contents");
                foreach (var file in Directory.GetFiles(backendSourcePath))
                {
                    var name = Path.GetFileName(file);
                    var isDylib = name.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase);
                    // The .dylibs need to go into the `*.app/Contents/Plugins` dirctory and will be picked
                    // up by unity. The crash handler (sentry-native) needs to be next to the game's executable
                    var desination = Path.Combine(contents, isDylib ? "PlugIns" : "MacOS", name);
                    yield return new NativePluginArtifact(
                        file,
                        desination,
                        isExecutable: !isDylib);
                }
                break;

            case BuildTarget.StandaloneLinux64:
                // No standalone crash handler for Linux - uses built-in handlers.
                break;
            case BuildTarget.GameCoreXboxSeries:
            case BuildTarget.GameCoreXboxOne:
                // No standalone crash handler for Xbox - comes with Breakpad
                break;
            case BuildTarget.PS5:
                // No standalone crash handler for PlayStation
                break;
            case BuildTarget.Switch:
                // No standalone crash handler for Switch - uses Nintendo's crash reporter
                break;
            default:
                throw new ArgumentException($"Unsupported build target: {target}");
        }
    }

    // On case-insensitive APFS, leftover artifacts from a prior build with
    // the *other* macOS backend break DllImport("sentry") resolution
    // (Sentry.dylib gets picked over libsentry.dylib, surfacing as
    // `sentry_options_new` not found at runtime). Wipe both candidates
    // before copying the current backend's files in.
    private static void CleanupStaleMacOSArtifacts(IDiagnosticLogger logger, string executablePath)
    {
        var contents = Path.Combine(executablePath, "Contents");
        foreach (var stale in new[]
        {
            Path.Combine(contents, "PlugIns", "Sentry.dylib"),
            Path.Combine(contents, "PlugIns", "libsentry.dylib"),
            Path.Combine(contents, "MacOS", "sentry-crash"),
        })
        {
            if (File.Exists(stale))
            {
                logger.LogDebug("Removing stale Sentry artifact from prior build: '{0}'", stale);
                File.Delete(stale);
            }
        }
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
                if (options.Experimental.MacosBackend == MacosBackend.Native)
                {
                    var packageMacOSDir = $"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/SentryNative~";
                    AddPath(paths, Path.GetFullPath($"{packageMacOSDir}/libsentry.dylib.dSYM"), logger);
                    AddPath(paths, Path.GetFullPath($"{packageMacOSDir}/sentry-crash.dSYM"), logger);
                }
                else
                {
                    AddPath(paths, Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/Sentry~/Sentry.dylib.dSYM"), logger);
                }

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

            case BuildTarget.Switch:
                // IL2CPP output, Managed DLLs/PDBs, and Symbols
                foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BackUpThisFolder_*"))
                {
                    AddPath(paths, dir, logger);
                }

                // Burst
                foreach (var dir in Directory.GetDirectories(buildOutputDir, "*_BurstDebugInformation_*"))
                {
                    AddPath(paths, dir, logger);
                }

                // When exporting as an NSP the assemblies are bundled inside the package. So we're also checking the build cache.
                var beePath = Path.Combine(projectDir, "Library", "Bee", "artifacts", "SwitchPlayerBuildProgram");
                AddPath(paths, beePath, logger);

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
