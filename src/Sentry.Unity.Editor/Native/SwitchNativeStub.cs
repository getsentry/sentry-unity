using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sentry.Extensibility;
using UnityEditor;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Keeps the no-op native stubs in the consumer project in step with the sentry-switch libraries
/// the user has installed.
/// </summary>
/// <remarks>
/// <para>
/// sentry-switch is distributed under NDA, so a Switch player has to link something for the
/// <c>__Internal</c> bindings in <c>Sentry.Unity.Native.Switch.dll</c> to resolve. The stubs are
/// that something, and they exist so the SDK never breaks a build it cannot complete.
/// </para>
/// <para>
/// The stub used to ship as a plugin inside the package, enabled by default, and a build callback
/// turned it off once the real libraries appeared. That cannot work: a package installed from a
/// registry or a Git URL lives in <c>Library/PackageCache</c>, which Unity treats as immutable, so
/// <c>PluginImporter.SaveAndReimport</c> never reaches disk. The stub stayed enabled, the linker
/// took it in preference to the archive, and the player shipped no-op crash reporting while the
/// build log said the real libraries had been found.
/// </para>
/// <para>
/// So the stub is no longer an asset in the package. It sits in <c>Plugins/Switch/SentryStub~</c>,
/// which the AssetDatabase does not import, and this class copies it into
/// <c>Assets/Plugins/Sentry/&lt;target&gt;</c> when the libraries for that target are missing and
/// deletes it once they are there. That location belongs to the user, so writing to it is allowed
/// whatever the package was installed from. It runs on domain reload and whenever the plugin folder
/// changes, never as part of a build, so the database has settled long before a build starts.
/// </para>
/// <para>
/// Only the active build target ever gains a stub. A copy is a file in someone's repository, and a
/// project that builds for Windows has no reason to carry one, nor has a Switch project any reason
/// to carry the Switch 2 copy. Switching platform reloads the domain, which is what brings the stub
/// along. Removal is not gated the same way: a stub whose libraries have arrived is deleted whichever
/// platform is selected, because leaving it is what shadows those libraries at link time.
/// </para>
/// </remarks>
internal static class SwitchNativeStub
{
    internal const string StubFileName = "sentry_native_stubs.c";

    /// <summary>The header line the stub stamps its version on.</summary>
    internal const string VersionMarker = "sentry-unity stub version:";

    private static bool _syncing;

    /// <summary>
    /// Whether a sync is under way, so the import it causes does not start another one.
    /// </summary>
    internal static bool IsSyncing => _syncing;

    /// <summary>
    /// The build target's name doubles as the directory name, i.e. `Switch` and `Switch2`.
    /// </summary>
    internal static string PluginDirectoryFor(BuildTarget target) => $"Assets/Plugins/Sentry/{target}";

    /// <summary>
    /// Both platforms share one stub, so the required libraries are what differ between them.
    /// </summary>
    internal static string[] RequiredFilesFor(BuildTarget target)
    {
        var directory = PluginDirectoryFor(target);
        return
        [
            $"{directory}/libsentry.a",
            $"{directory}/libzstd.a"
        ];
    }

    internal static string StubPathFor(BuildTarget target) => $"{PluginDirectoryFor(target)}/{StubFileName}";

    /// <summary>
    /// Every Switch build target this Editor knows about. Resolved by enumeration because
    /// <c>BuildTarget.Switch2</c> does not exist before Unity 6000.3.
    /// </summary>
    internal static IEnumerable<BuildTarget> Targets => Enum
        .GetValues(typeof(BuildTarget))
        .Cast<BuildTarget>()
        .Where(target => target.IsSwitchFamily());

    /// <summary>
    /// Whether the given target's stub on disk matches its libraries, i.e. present exactly when one
    /// of them is missing, and carrying what the installed SDK version would write.
    /// </summary>
    /// <remarks>
    /// The stamped version counts, not just presence. A stub left over from an older SDK is the case
    /// the stubs exist to prevent: a binding added since it was written has nothing to resolve
    /// against, and the player fails to link on a symbol nobody has heard of.
    /// </remarks>
    internal static bool IsInSync(BuildTarget target)
    {
        var stubPath = StubPathFor(target);
        var stubExists = File.Exists(stubPath);

        if (stubExists != RequiredFilesFor(target).Any(file => !File.Exists(file)))
        {
            return false;
        }

        // Nothing to compare against if the package cannot be read. AddStub reports that.
        return !stubExists
               || !TryReadTemplate(null, out var template)
               || Matches(File.ReadAllText(stubPath), template);
    }

    /// <summary>
    /// The version stamped in a stub's header comment, or null if it carries none.
    /// </summary>
    /// <remarks>
    /// A stamp rather than a comparison of the whole file. Source control normalizes line endings on
    /// checkout, so comparing content literally would rewrite the copy on every domain reload and
    /// leave the tree permanently dirty. It also gives a number to ask for in a bug report.
    /// </remarks>
    internal static string? VersionOf(string content)
    {
        var match = Regex.Match(content, Regex.Escape(VersionMarker) + @"\s*(\S+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Whether a copy is the one this SDK version would write. A copy with no stamp at all predates
    /// the stamp or was written by hand, and is replaced either way.
    /// </summary>
    private static bool Matches(string stub, string template)
    {
        var wanted = VersionOf(template);
        return wanted is null || VersionOf(stub) == wanted;
    }

    private static bool TryReadTemplate(IDiagnosticLogger? logger, out string template)
    {
        try
        {
            template = File.ReadAllText(TemplatePath());
            return true;
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to read the Switch stub template from the Sentry package.");
            template = string.Empty;
            return false;
        }
    }

    [InitializeOnLoadMethod]
    private static void OnDomainReload() => EditorApplication.delayCall += () => Sync(null);

    /// <summary>
    /// Brings the stubs in line with the installed libraries, adding one for the active build target
    /// when it is a Switch target without them and removing any whose libraries have arrived.
    /// </summary>
    internal static void Sync(IDiagnosticLogger? logger) =>
        Sync(logger, EditorUserBuildSettings.activeBuildTarget);

    internal static void Sync(IDiagnosticLogger? logger, BuildTarget activeTarget)
    {
        if (_syncing)
        {
            return;
        }

        // Deliberately not SentryScriptableObject.LoadOptions() on this path. It runs the consumer's
        // Configure callback, and a domain reload is no reason to do that.
        logger ??= new UnityLogger(new SentryUnityOptions());

        _syncing = true;
        try
        {
            foreach (var target in Targets)
            {
                SyncTarget(logger, target, mayAdd: target == activeTarget);
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private static void SyncTarget(IDiagnosticLogger logger, BuildTarget target, bool mayAdd)
    {
        var requiredFiles = RequiredFilesFor(target);
        var presentFiles = requiredFiles.Where(File.Exists).ToList();
        var missingFiles = requiredFiles.Except(presentFiles).ToList();

        if (missingFiles.Count == 0)
        {
            RemoveStub(logger, target);
            return;
        }

        // Nothing to link for a target nobody is building, so nothing to put in their repository
        // either. The stub arrives with the platform switch, which reloads the domain.
        if (!mayAdd)
        {
            return;
        }

        // A half-installed set links no better than none at all, so the stub stays and stands in for
        // the whole set. Saying so is the point: the user meant to enable native support and has not.
        if (presentFiles.Count > 0)
        {
            logger.LogWarning(
                "{0} native support is partially configured. Missing files:\n{1}\n" +
                "Sentry's no-op stubs are being used instead. Add all required files to enable native " +
                "support, or remove all of them to silence this warning. " +
                "Build sentry-switch and copy the libraries to the expected locations. " +
                "See: https://github.com/getsentry/sentry-switch",
                target, string.Join("\n", missingFiles.Select(file => $"  - {file}")));
        }

        AddStub(logger, target);
    }

    private static void AddStub(IDiagnosticLogger logger, BuildTarget target)
    {
        var stubPath = StubPathFor(target);

        if (!TryReadTemplate(logger, out var template))
        {
            logger.LogError("Native calls will not link for '{0}' without the stubs.", target);
            return;
        }

        var existing = File.Exists(stubPath);
        if (existing && Matches(File.ReadAllText(stubPath), template))
        {
            // The importer is still worth checking. It is the part a user can change by hand, and a
            // stub that leaks into another platform's build is a link error there.
            ConfigureImporter(logger, stubPath, target);
            return;
        }

        // Overwriting on purpose, including a copy someone has edited. The stub tracks the bindings in
        // Sentry.Unity.Native.Switch.dll, so an SDK upgrade that adds one has to bring its stub along
        // or the next Switch build fails to link. The copy is generated; the original lives in the
        // package, and its stamped version is what said this one is behind.
        Directory.CreateDirectory(Path.GetDirectoryName(stubPath)!);
        File.WriteAllText(stubPath, template);
        // Refresh rather than ImportAsset, because the plugin directory itself may be new and an
        // unimported folder has no assets in it as far as the database is concerned.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(logger, stubPath, target);

        if (existing)
        {
            logger.LogInfo("{0} no-op stubs in '{1}' updated to stub version {2}.",
                target, PluginDirectoryFor(target), VersionOf(template) ?? "unstamped");
            return;
        }

        logger.LogInfo(
            "{0} native libraries not found in '{1}' - added Sentry's no-op stubs so the player links. " +
            "Native crash reporting is unavailable until sentry-switch is installed there.",
            target, PluginDirectoryFor(target));
    }

    private static void RemoveStub(IDiagnosticLogger logger, BuildTarget target)
    {
        var stubPath = StubPathFor(target);
        if (!File.Exists(stubPath))
        {
            return;
        }

        // The archive is only searched for symbols that are still undefined, so leaving the stub next
        // to it does not merely duplicate the library, it replaces it.
        if (!AssetDatabase.DeleteAsset(stubPath))
        {
            logger.LogError(
                "Failed to delete '{0}'. The stub takes linker precedence over sentry-switch, so native " +
                "support stays disabled while it is there. Delete it by hand and rebuild.", stubPath);
            return;
        }

        logger.LogInfo("{0} native libraries found in '{1}' - removed Sentry's no-op stubs.",
            target, PluginDirectoryFor(target));
    }

    /// <summary>
    /// Scopes the stub to the one target it stands in for. A C source plugin imports as compatible
    /// with everything by default, which would compile it into every other platform's player.
    /// </summary>
    private static void ConfigureImporter(IDiagnosticLogger logger, string stubPath, BuildTarget target)
    {
        if (AssetImporter.GetAtPath(stubPath) is not PluginImporter importer)
        {
            logger.LogError("Failed to get the PluginImporter for '{0}'. Skipping stub configuration.", stubPath);
            return;
        }

        // Disabling "Any Platform" leaves only the platforms enabled one by one, so the siblings are
        // the ones worth being explicit about. A Switch stub compiled into a Switch 2 player would
        // shadow that target's libraries exactly the way the packaged stub used to.
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
/// Reacts to the user adding or removing the sentry-switch libraries, so the stubs follow without
/// anyone having to think about it.
/// </summary>
internal class SwitchNativeStubWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (SwitchNativeStub.IsSyncing)
        {
            return;
        }

        var libraries = importedAssets
            .Concat(deletedAssets)
            .Concat(movedAssets)
            .Concat(movedFromAssetPaths)
            .Any(path => path.EndsWith("libsentry.a", StringComparison.Ordinal)
                         || path.EndsWith("libzstd.a", StringComparison.Ordinal));

        if (libraries)
        {
            EditorApplication.delayCall += () => SwitchNativeStub.Sync(null);
        }
    }
}
