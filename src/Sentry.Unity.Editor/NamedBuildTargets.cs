using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor;

/// <summary>
/// Dedicated Server builds report <c>BuildTargetGroup.Standalone</c> but have their own player settings,
/// i.e. scripting backend, under <c>NamedBuildTarget.Server</c>.
/// </summary>
internal static class NamedBuildTargets
{
    internal static NamedBuildTarget FromBuildReport(BuildReport report) =>
        From(report.summary.platformGroup, report.summary.GetSubtarget<StandaloneBuildSubtarget>());

    internal static NamedBuildTarget From(BuildTargetGroup group, StandaloneBuildSubtarget subtarget) =>
        group == BuildTargetGroup.Standalone && subtarget == StandaloneBuildSubtarget.Server
            ? NamedBuildTarget.Server
            : NamedBuildTarget.FromBuildTargetGroup(group);
}
