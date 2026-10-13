using System;
using UnityEditor;

namespace Sentry.Unity.Editor.Native;

/// <summary>
/// <c>BuildTarget.Switch2</c> and <c>BuildTargetGroup.Switch2</c> only exist in Unity 6000.3 
/// and newer, so matching by name instead.
/// </summary>
internal static class SwitchBuildTargets
{
    private const string Switch2Name = "Switch2";

    internal static bool IsSwitch2(this BuildTarget target) =>
        string.Equals(target.ToString(), Switch2Name, StringComparison.Ordinal);

    internal static bool IsSwitch2(this BuildTargetGroup group) =>
        string.Equals(group.ToString(), Switch2Name, StringComparison.Ordinal);

    internal static bool IsSwitchFamily(this BuildTarget target) =>
        target is BuildTarget.Switch || target.IsSwitch2();
}
