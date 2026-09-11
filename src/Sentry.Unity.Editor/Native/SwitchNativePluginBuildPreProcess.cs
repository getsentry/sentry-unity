using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Checks, at the start of a Switch build, that the no-op stubs match the libraries installed in the
/// project.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SwitchNativeStub"/> keeps the two in step on domain reload and whenever the plugin
/// folder changes, so by the time a build starts there is normally nothing to do. This is the guard
/// for when that did not happen, a stub left behind by an older SDK version among them.
/// </para>
/// <para>
/// It deliberately does not repair and carry on. Unity collects native plugins as the build starts,
/// so an asset written from a build callback may or may not reach the player, and the version that
/// does is the one nobody can see. Fixing the tree and asking for another build is the only honest
/// answer, and it fails towards the case where a stubbed player would otherwise have shipped looking
/// healthy.
/// </para>
/// </remarks>
internal class SwitchNativePluginBuildPreProcess : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (!report.summary.platform.IsSwitchFamily())
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        var logger = options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());

        Validate(logger, report.summary.platform);
    }

    internal static void Validate(IDiagnosticLogger logger, BuildTarget target)
    {
        if (SwitchNativeStub.IsInSync(target))
        {
            return;
        }

        // The target being built is the active one by the time a build runs, but saying which one
        // matters here costs nothing and does not depend on that staying true.
        SwitchNativeStub.Sync(logger, target);

        throw new BuildFailedException(
            "Sentry's Switch native plugins were out of step with the libraries in " +
            "'Assets/Plugins/Sentry'. They have been corrected - please build again.\n" +
            "This guards against shipping a player that links Sentry's no-op stubs while the real " +
            "sentry-switch libraries sit next to them, which reports no crashes at all.");
    }
}
