using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Android;

internal static class AndroidUtils
{
    internal static bool ShouldUploadMapping()
    {
        var isDebug = EditorUserBuildSettings.development;
        var majorVersion = int.Parse(Application.unityVersion.Split('.')[0]);
        if (majorVersion < 2020)
        {
            var buildSettingsType = typeof(EditorUserBuildSettings);
            var propertyName = isDebug ? "androidDebugMinification" : "androidReleaseMinification";
            var prop = buildSettingsType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
            {
                var value = (int)prop.GetValue(null);
                return value > 0;
            }
        }
        else
        {
            var type = typeof(PlayerSettings.Android);
            var propertyName = isDebug ? "minifyDebug" : "minifyRelease";
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (prop != null)
            {
                return (bool)prop.GetValue(null);
            }
        }

        return false;
    }
}
