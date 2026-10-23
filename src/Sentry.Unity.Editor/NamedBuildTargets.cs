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
    // `GetSubtarget` throws on platforms without a subtarget, e.g. Android.
    internal static NamedBuildTarget FromBuildReport(BuildReport report) =>
        report.summary.platformGroup == BuildTargetGroup.Standalone
            ? From(BuildTargetGroup.Standalone, report.summary.GetSubtarget<StandaloneBuildSubtarget>())
            : NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);

    internal static NamedBuildTarget From(BuildTargetGroup group, StandaloneBuildSubtarget subtarget) =>
        group == BuildTargetGroup.Standalone && subtarget == StandaloneBuildSubtarget.Server
            ? NamedBuildTarget.Server
            : NamedBuildTarget.FromBuildTargetGroup(group);
}
