using System;
using UnityEditor;
using UnityEditor.Build;

namespace Sentry.Unity.Editor;

internal static class SentryBuildDefines
{
    internal const string Disabled = "SENTRY_UNITY_DISABLED";

    internal static bool IsDisabled(BuildTarget target) =>
        IsDisabled(BuildPipeline.GetBuildTargetGroup(target));

    internal static bool IsDisabled(BuildTargetGroup targetGroup) =>
        IsDisabled(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup)));

    internal static bool IsDisabled(string defines) =>
        Array.IndexOf(defines.Split(';'), Disabled) >= 0;
}
