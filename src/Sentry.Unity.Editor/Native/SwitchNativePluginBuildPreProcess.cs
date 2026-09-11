using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// Defensively fails a Switch build if the no-op stubs are not set up.
/// </summary>
/// <remarks>
/// <see cref="SwitchNativeStub"/> should have set this up already. This is the guard if it does not.
/// We cannot repair the setup here. Unity collects native plugins as the build starts, so writing
/// an asset from a build callback may or may not reach the player.
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
        var target = report.summary.platform;

        if (SwitchNativeStub.IsInSync(target))
        {
            return;
        }

        SwitchNativeStub.Sync(logger, target);

        throw new BuildFailedException(
            "Sentry's Switch no-op stubs in 'Assets/Plugins/Sentry' were out of step with the " +
            "installed libraries and have been updated. Please trigger the build again.");
    }
}
