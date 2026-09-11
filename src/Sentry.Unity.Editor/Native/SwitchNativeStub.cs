using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sentry.Extensibility;
using UnityEditor;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Keeps the no-op native stubs in the user's project up to date with the sentry-switch libraries 
/// they have installed.
/// </summary>
/// <remarks>
/// <para>
/// Only the active build target gains a copy, so projects do not carry a file they have no use for.
/// Removal is not gated that way, because a stale stub shadows the libraries next to it.
/// </para>
/// </remarks>
internal static class SwitchNativeStub
{
    internal const string StubFileName = "sentry_native_stubs.c";

    /// <summary>
    /// The sentry-switch libraries the stub stands in for. The watcher below keys off these names.
    /// </summary>
    internal static readonly string[] LibraryFileNames = ["libsentry.a", "libzstd.a"];

    /// <summary>
    /// The build target's name doubles as the directory name, i.e. `Switch` and `Switch2`.
    /// </summary>
    internal static string PluginDirectoryFor(BuildTarget target) => $"Assets/Plugins/Sentry/{target}";

    /// <summary>
    /// Both platforms share one stub. The difference is the directory name the stub goes in.
    /// </summary>
    internal static string[] RequiredFilesFor(BuildTarget target)
    {
        var directory = PluginDirectoryFor(target);
        return LibraryFileNames.Select(library => $"{directory}/{library}").ToArray();
    }

    internal static string StubPathFor(BuildTarget target) => $"{PluginDirectoryFor(target)}/{StubFileName}";

    /// <summary>
    /// Resolved by enumeration because <c>BuildTarget.Switch2</c> does not exist in Unity 6000.2 or older.
    /// </summary>
    internal static IEnumerable<BuildTarget> Targets => Enum
        .GetValues(typeof(BuildTarget))
        .Cast<BuildTarget>()
        .Where(target => target.IsSwitchFamily());

    /// <summary>
    /// Whether the stub is needed and matches the packaged version.
    /// </summary>
    /// <remarks>
    /// Checks the content. An older stub might miss new API.
    /// </remarks>
    internal static bool IsInSync(BuildTarget target)
    {
        var stubPath = StubPathFor(target);
        var stubNeeded = RequiredFilesFor(target).Any(file => !File.Exists(file));

        if (!File.Exists(stubPath))
        {
            return !stubNeeded;
        }

        return stubNeeded && Matches(File.ReadAllText(stubPath), File.ReadAllText(TemplatePath()));
    }

    private static bool Matches(string stub, string template) =>
        string.Equals(Normalize(stub), Normalize(template), StringComparison.Ordinal);

    private static string Normalize(string content) =>
        content.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();

    [InitializeOnLoadMethod]
    private static void OnDomainReload() => EditorApplication.delayCall += () => Sync(null);

    /// <summary>
    /// Syncs the SDK with the state of the game. Adds/removes the stub.
    /// </summary>
    internal static void Sync(IDiagnosticLogger? logger) =>
        Sync(logger, EditorUserBuildSettings.activeBuildTarget);

    internal static void Sync(IDiagnosticLogger? logger, BuildTarget activeTarget)
    {
        // Running inside a domain reload or an asset change callback.
        logger ??= new UnityLogger(new SentryUnityOptions());

        foreach (var target in Targets)
        {
            var missingFiles = RequiredFilesFor(target).Where(file => !File.Exists(file)).ToList();

            if (missingFiles.Count == 0)
            {
                RemoveStub(logger, target);
            }
            // No reason to put a file in the user's repository for a target they are not building.
            else if (target == activeTarget)
            {
                WarnAboutPartialInstall(logger, target, missingFiles);
                AddStub(logger, target);
            }
        }
    }

    private static void WarnAboutPartialInstall(
        IDiagnosticLogger logger, BuildTarget target, List<string> missingFiles)
    {
        if (missingFiles.Count == LibraryFileNames.Length)
        {
            return;
        }

        logger.LogWarning(
            "{0} native support is partially configured. Missing files:\n{1}\n" +
            "Sentry's no-op stubs are being used instead. Add all required files to enable native " +
            "support, or remove all of them to silence this warning. " +
            "Build sentry-switch and copy the libraries to the expected locations. " +
            "See: https://github.com/getsentry/sentry-switch",
            target, string.Join("\n", missingFiles.Select(file => $"  - {file}")));
    }

    private static void AddStub(IDiagnosticLogger logger, BuildTarget target)
    {
        var stubPath = StubPathFor(target);
        var template = File.ReadAllText(TemplatePath());

        var existing = File.Exists(stubPath);
        if (existing && Matches(File.ReadAllText(stubPath), template))
        {
            // Manually set the stub's targeted platform.
            ConfigureImporter(logger, stubPath, target);
            return;
        }

        _ = Directory.CreateDirectory(Path.GetDirectoryName(stubPath)!);
        File.WriteAllText(stubPath, template);
        // Refresh because the plugin directory itself may be new to the AssetDatabase.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(logger, stubPath, target);

        if (existing)
        {
            logger.LogInfo("{0} no-op stubs in '{1}' updated to the version this SDK ships.",
                target, PluginDirectoryFor(target));
            return;
        }

        logger.LogInfo(
            "{0} native libraries not found in '{1}'. Added no-op stubs.",
            target, PluginDirectoryFor(target));
    }

    private static void RemoveStub(IDiagnosticLogger logger, BuildTarget target)
    {
        var stubPath = StubPathFor(target);
        if (!File.Exists(stubPath))
        {
            return;
        }

        if (!AssetDatabase.DeleteAsset(stubPath))
        {
            logger.LogError(
                "Failed to delete '{0}'. Native support might fail at runtime.\n" +
                "Delete it by hand and rebuild.", stubPath);
            return;
        }

        logger.LogInfo("{0} native libraries found in '{1}'. Removed no-op stubs.",
            target, PluginDirectoryFor(target));
    }

    /// <summary>
    /// The stub should only target the platform for the directory's name it is in. `.c` targets every platform
    /// by default.
    /// </summary>
    private static void ConfigureImporter(IDiagnosticLogger logger, string stubPath, BuildTarget target)
    {
        if (AssetImporter.GetAtPath(stubPath) is not PluginImporter importer)
        {
            logger.LogError("Failed to get the PluginImporter for '{0}'. Skipping stub configuration.", stubPath);
            return;
        }

        if (!importer.GetCompatibleWithAnyPlatform() &&
            !importer.GetCompatibleWithEditor() &&
            Targets.All(candidate => importer.GetCompatibleWithPlatform(candidate) == (candidate == target)))
        {
            return;
        }

        importer.SetCompatibleWithAnyPlatform(false);
        importer.SetCompatibleWithEditor(false);
        foreach (var candidate in Targets)
        {
            importer.SetCompatibleWithPlatform(candidate, candidate == target);
        }

        importer.SaveAndReimport();
    }

    private static string TemplatePath() => Path.GetFullPath(Path.Combine(
        "Packages", SentryPackageInfo.GetName(), "Plugins", "Switch", "SentryStub~", StubFileName));
}

/// <summary>
/// Monitor the imported Assets so we can replace the stubs when necessary.
/// </summary>
internal class SwitchNativeStubWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var librariesChanged = importedAssets
            .Concat(deletedAssets)
            .Concat(movedAssets)
            .Concat(movedFromAssetPaths)
            .Any(path => SwitchNativeStub.LibraryFileNames.Any(
                library => path.EndsWith(library, StringComparison.Ordinal)));

        if (librariesChanged)
        {
            EditorApplication.delayCall += () => SwitchNativeStub.Sync(null);
        }
    }
}
