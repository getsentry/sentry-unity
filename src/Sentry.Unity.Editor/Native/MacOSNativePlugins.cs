using System;
using System.Collections.Generic;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Ships the macOS backend's binaries as Unity plugins, so Unity places them in the app before it signs it.
/// </summary>
/// <remarks>
/// Each backend's plugins are enabled for the macOS player. The include-in-build delegate leaves out the
/// plugins of the backend that isn't selected, and all of them when native support is off.
/// </remarks>
[InitializeOnLoad]
internal static class MacOSNativePlugins
{
    internal const string CocoaLibrary = "Sentry/Sentry.dylib";
    internal const string NativeLibrary = "SentryNative/libsentry-native.dylib";
    internal const string CrashBundle = "SentryNative/SentryCrash.bundle";
    internal const string CrashExecutable = CrashBundle + "/Contents/MacOS/sentry-crash";

    private const string PluginsFolder = "/Plugins/macOS/";

    private static readonly string[] Plugins = { CocoaLibrary, NativeLibrary, CrashBundle };

    // The options of the build in progress, so the delegate doesn't load them for every plugin.
    private static SentryUnityOptions? BuildOptions;

    static MacOSNativePlugins()
    {
        // The delegate doesn't survive a domain reload, and the asset database isn't ready this early.
        EditorApplication.delayCall += () => Register(null);
    }

    internal static string PackagePath(string plugin) => $"Packages/{SentryPackageInfo.GetName()}/Plugins/macOS/{plugin}";

    /// <summary>
    /// Sets the include-in-build delegate on each plugin, and returns the asset paths it found no plugin at.
    /// </summary>
    internal static List<string> Register(IDiagnosticLogger? logger)
    {
        var missing = new List<string>();
        foreach (var plugin in Plugins)
        {
            var assetPath = PackagePath(plugin);
            if (AssetImporter.GetAtPath(assetPath) is PluginImporter importer)
            {
                importer.SetIncludeInBuildDelegate(IncludeInBuild);
            }
            else
            {
                logger?.LogDebug("No macOS plugin at '{0}'.", assetPath);
                missing.Add(assetPath);
            }
        }

        return missing;
    }

    internal static void SetBuildOptions(SentryUnityOptions? options) => BuildOptions = options;

    private static bool IncludeInBuild(string assetPath)
    {
        var options = BuildPipeline.isBuildingPlayer && BuildOptions is not null
            ? BuildOptions
            : SentryScriptableObject.LoadOptions(isBuilding: true);
        return ShouldIncludeInBuild(assetPath, options);
    }

    /// <summary>
    /// The backend a plugin belongs to, or <c>null</c> when the path isn't one of Sentry's macOS plugins.
    /// </summary>
    internal static MacosBackend? BackendOf(string assetPath)
    {
        var normalized = assetPath.Replace('\\', '/');
        if (normalized.EndsWith(PluginsFolder + CocoaLibrary, StringComparison.Ordinal))
        {
            return MacosBackend.Cocoa;
        }

        if (normalized.EndsWith(PluginsFolder + NativeLibrary, StringComparison.Ordinal)
            || normalized.EndsWith(PluginsFolder + CrashBundle, StringComparison.Ordinal))
        {
            return MacosBackend.Native;
        }

        return null;
    }

    internal static MacosBackend? SelectedBackend(SentryUnityOptions? options) =>
        options is not null && options.IsValid() && options.MacosNativeSupportEnabled
            ? options.Experimental.MacosBackend
            : null;

    /// <summary>
    /// The files a backend needs in the package, relative to <c>Plugins/macOS</c>.
    /// </summary>
    internal static string[] RequiredFiles(MacosBackend? backend) => backend switch
    {
        MacosBackend.Cocoa => new[] { CocoaLibrary },
        MacosBackend.Native => new[] { NativeLibrary, CrashExecutable },
        _ => Array.Empty<string>()
    };

    internal static string MissingFileMessage(MacosBackend backend, string path, bool isDevPackage)
    {
        var restore = isDevPackage
            ? $"Run 'dotnet msbuild /t:{(backend == MacosBackend.Native ? "BuildMacOSNativeSDK" : "BuildCocoaSDK")} src/Sentry.Unity' " +
              "(or 'dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity') to populate it"
            : "Reinstall the Sentry package";
        return $"Sentry's macOS {backend} backend file is missing: {path}\n" +
               $"{restore}, or turn off macOS native support in the Sentry options.";
    }

    internal static bool ShouldIncludeInBuild(string assetPath, SentryUnityOptions? options)
    {
        var backend = BackendOf(assetPath);
        return backend is null || backend == SelectedBackend(options);
    }

    /// <summary>
    /// What an earlier build may have left in the app that this build doesn't ship: the other backend's
    /// plugins, and the files older SDK versions copied into the app after Unity signed it.
    /// </summary>
    internal static IEnumerable<string> StaleArtifacts(string appPath, SentryUnityOptions? options)
    {
        var contents = Path.Combine(appPath, "Contents");
        var pluginsDir = Path.Combine(contents, "PlugIns");
        var backend = SelectedBackend(options);

        if (backend != MacosBackend.Cocoa)
        {
            yield return Path.Combine(pluginsDir, Path.GetFileName(CocoaLibrary));
        }

        if (backend != MacosBackend.Native)
        {
            yield return Path.Combine(pluginsDir, Path.GetFileName(NativeLibrary));
            yield return Path.Combine(pluginsDir, Path.GetFileName(CrashBundle));
        }

        yield return Path.Combine(pluginsDir, "libsentry.dylib");
        yield return Path.Combine(contents, "MacOS", "sentry-crash");
    }

    internal static void RemoveStaleArtifacts(IDiagnosticLogger logger, string appPath, SentryUnityOptions? options)
    {
        foreach (var stale in StaleArtifacts(appPath, options))
        {
            if (Directory.Exists(stale))
            {
                logger.LogDebug("Removing '{0}' left by an earlier build.", stale);
                Directory.Delete(stale, recursive: true);
            }
            else if (File.Exists(stale))
            {
                logger.LogDebug("Removing '{0}' left by an earlier build.", stale);
                File.Delete(stale);
            }
        }
    }

    /// <summary>
    /// Every copy of sentry-crash in a built app, or in an exported Xcode project.
    /// </summary>
    internal static IEnumerable<string> BuiltCrashExecutables(string outputPath)
    {
        if (!Directory.Exists(outputPath))
        {
            yield break;
        }

        foreach (var bundle in Directory.GetDirectories(outputPath, Path.GetFileName(CrashBundle), SearchOption.AllDirectories))
        {
            var executable = Path.Combine(bundle, "Contents", "MacOS", "sentry-crash");
            if (File.Exists(executable))
            {
                yield return executable;
            }
        }
    }
}

internal class MacOSNativePluginsBuildProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    // Before Unity collects the plugins.
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneOSX)
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        var logger = options?.DiagnosticLogger ?? new UnityLogger(options ?? new SentryUnityOptions());

        MacOSNativePlugins.SetBuildOptions(options);
        foreach (var assetPath in MacOSNativePlugins.Register(logger))
        {
            // The plugin may still be in the project somewhere else, and then it ships whatever the backend.
            logger.LogWarning("Sentry found no macOS plugin at '{0}', so it can't leave it out of the build " +
                              "when its backend isn't selected.", assetPath);
        }

        var backend = MacOSNativePlugins.SelectedBackend(options);
        foreach (var file in MacOSNativePlugins.RequiredFiles(backend))
        {
            var path = Path.GetFullPath(MacOSNativePlugins.PackagePath(file));
            if (!File.Exists(path))
            {
                throw new BuildFailedException(
                    MacOSNativePlugins.MissingFileMessage(backend!.Value, path, SentryPackageInfo.IsDevPackage));
            }
        }

        // Unity builds into an existing app, so another backend's plugins would stay behind. Removed here,
        // before Unity signs the app.
        var outputPath = report.summary.outputPath;
        if (outputPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase) && Directory.Exists(outputPath))
        {
            MacOSNativePlugins.RemoveStaleArtifacts(logger, outputPath, options);
        }

        if (backend == MacosBackend.Native)
        {
            // Zip archives and artifact uploads can lose the execute bit, and Unity copies the bundle as it is.
            var executable = Path.GetFullPath(MacOSNativePlugins.PackagePath(MacOSNativePlugins.CrashExecutable));
            try
            {
                SentryCli.SetExecutePermission(executable);
            }
            catch (Exception e)
            {
                throw new BuildFailedException(
                    $"Sentry failed to make '{executable}' executable ({e.Message}). " +
                    $"Run 'chmod +x \"{executable}\"' and build again.");
            }
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        MacOSNativePlugins.SetBuildOptions(null);

        if (report.summary.platform != BuildTarget.StandaloneOSX)
        {
            return;
        }

        // The pre-build makes the package's copy executable. Unity copies the mode along, so this only changes
        // anything when the copy didn't keep it, and the mode isn't part of the signature.
        foreach (var executable in MacOSNativePlugins.BuiltCrashExecutables(report.summary.outputPath))
        {
            SentryCli.SetExecutePermission(executable);
        }
    }
}
