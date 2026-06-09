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
            else if (target is BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64)
            {
                // Since the backend can change between iterative builds we need to clean up after ourselves
                CleanupStaleWindowsArtifacts(logger, buildOutputDir);
            }
            else if (target == BuildTarget.StandaloneLinux64)
            {
                // Since the backend can change between iterative builds we need to clean up after ourselves
                CleanupStaleLinuxArtifacts(logger, buildOutputDir);
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
                var windowsBackendSourcePath = options.Experimental.WindowsBackend == WindowsBackend.Native
                    ? Path.Combine(pluginsPath, "Windows", "SentryNative~")
                    : Path.Combine(pluginsPath, "Windows", "Sentry~");
                if (!Directory.Exists(windowsBackendSourcePath))
                {
                    var buildTarget = options.Experimental.WindowsBackend == WindowsBackend.Native ? "BuildWindowsNativeSDK" : "BuildWindowsSDK";
                    throw new BuildFailedException(
                        $"Sentry Windows plugin directory not found: {windowsBackendSourcePath}\n" +
                        $"Run 'dotnet msbuild /t:{buildTarget} src/Sentry.Unity' (or 'dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity') to populate it.");
                }
                // Flat copy of every non-PDB file next to the player .exe — sentry.dll and the
                // crash handler (crashpad_handler.exe / sentry-crash.exe) all sit at the build root.
                // PDBs stay in the package and are consumed at symbol-upload time only.
                foreach (var file in Directory.GetFiles(windowsBackendSourcePath))
                {
                    if (file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    yield return new NativePluginArtifact(
                        file,
                        Path.Combine(buildOutputDir, Path.GetFileName(file)));
                }
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
                var linuxBackendSourcePath = options.Experimental.LinuxBackend == LinuxBackend.Native
                    ? Path.Combine(pluginsPath, "Linux", "SentryNative~")
                    : Path.Combine(pluginsPath, "Linux", "Sentry~");
                if (!Directory.Exists(linuxBackendSourcePath))
                {
                    var buildTarget = options.Experimental.LinuxBackend == LinuxBackend.Native ? "BuildLinuxNativeSDK" : "BuildLinuxSDK";
                    throw new BuildFailedException(
                        $"Sentry Linux plugin directory not found: {linuxBackendSourcePath}\n" +
                        $"Run 'dotnet msbuild /t:{buildTarget} src/Sentry.Unity' (or 'dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity') to populate it.");
                }
                // libsentry.so must sit in the player's native plugin dir (<name>_Data/Plugins/x86_64) where the
                // Linux player resolves DllImport("sentry"). The crash daemon (sentry-crash, native backend only)
                // sits next to the player executable so sentry-native can spawn it on crash.
                // The .dbg.so / .dbg debug sidecars stay in the package and are consumed at symbol-upload time only.
                var linuxPluginDir = GetLinuxPluginDir(buildOutputDir);
                foreach (var file in Directory.GetFiles(linuxBackendSourcePath))
                {
                    var name = Path.GetFileName(file);
                    if (name.EndsWith(".dbg.so", StringComparison.OrdinalIgnoreCase)
                        || name.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var isSharedObject = name.EndsWith(".so", StringComparison.OrdinalIgnoreCase);
                    yield return new NativePluginArtifact(
                        file,
                        isSharedObject
                            ? Path.Combine(linuxPluginDir, name)
                            : Path.Combine(buildOutputDir, name),
                        isExecutable: !isSharedObject);
                }
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

    // Switching Windows backends between iterative builds leaves the other
    // backend's crash handler next to the player .exe (e.g. crashpad_handler.exe
    // lingers after switching to sentry-native). Wipe known handlers from both
    // backends before copying the current backend's files in.
    private static void CleanupStaleWindowsArtifacts(IDiagnosticLogger logger, string buildOutputDir)
    {
        foreach (var stale in new[]
        {
            Path.Combine(buildOutputDir, "crashpad_handler.exe"),
            Path.Combine(buildOutputDir, "crashpad_wer.dll"),
            Path.Combine(buildOutputDir, "sentry-crash.exe"),
            Path.Combine(buildOutputDir, "sentry-wer.dll"),
        })
        {
            if (File.Exists(stale))
            {
                logger.LogDebug("Removing stale Sentry artifact from prior build: '{0}'", stale);
                File.Delete(stale);
            }
        }
    }

    // Unity places Linux native plugins under <PlayerName>_Data/Plugins/x86_64, which the player adds
    // to its dlopen search path. We resolve the data dir by globbing (the player name isn't known here).
    private static string GetLinuxPluginDir(string buildOutputDir)
    {
        var dataDir = Directory.GetDirectories(buildOutputDir, "*_Data").FirstOrDefault();
        if (dataDir is null)
        {
            throw new BuildFailedException(
                $"Could not locate the player '*_Data' directory under '{buildOutputDir}' to place the Sentry native plugin.");
        }

        return Path.Combine(dataDir, "Plugins", "x86_64");
    }

    // Switching Linux backends between iterative builds leaves the other backend's artifacts behind
    // (a stale sentry-crash next to the player, or the other backend's libsentry.so in the plugin dir).
    // Wipe them before copying the current backend's files in.
    private static void CleanupStaleLinuxArtifacts(IDiagnosticLogger logger, string buildOutputDir)
    {
        var stalePaths = new List<string> { Path.Combine(buildOutputDir, "sentry-crash") };
        var dataDir = Directory.GetDirectories(buildOutputDir, "*_Data").FirstOrDefault();
        if (dataDir is not null)
        {
            stalePaths.Add(Path.Combine(dataDir, "Plugins", "x86_64", "libsentry.so"));
        }

        foreach (var stale in stalePaths)
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

                // Sentry native SDK symbols from package.
                // Glob *.pdb from whichever backend's source dir is in use, so adding
                // or removing PDBs at build time doesn't require touching this code.
                var windowsBackendDir = options.Experimental.WindowsBackend == WindowsBackend.Native
                    ? "SentryNative~"
                    : "Sentry~";
                var windowsPdbDir = Path.GetFullPath(
                    $"Packages/{SentryPackageInfo.GetName()}/Plugins/Windows/{windowsBackendDir}");
                if (Directory.Exists(windowsPdbDir))
                {
                    foreach (var pdb in Directory.GetFiles(windowsPdbDir, "*.pdb"))
                    {
                        AddPath(paths, pdb, logger);
                    }
                }

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

                // Sentry native SDK symbols from package.
                // Glob the debug sidecars (libsentry.dbg.so, and sentry-crash.dbg for the native backend)
                // from whichever backend's source dir is in use.
                var linuxBackendDir = options.Experimental.LinuxBackend == LinuxBackend.Native
                    ? "SentryNative~"
                    : "Sentry~";
                var linuxSymbolDir = Path.GetFullPath(
                    $"Packages/{SentryPackageInfo.GetName()}/Plugins/Linux/{linuxBackendDir}");
                if (Directory.Exists(linuxSymbolDir))
                {
                    foreach (var file in Directory.GetFiles(linuxSymbolDir))
                    {
                        if (file.EndsWith(".dbg.so", StringComparison.OrdinalIgnoreCase)
                            || file.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase))
                        {
                            AddPath(paths, file, logger);
                        }
                    }
                }

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
