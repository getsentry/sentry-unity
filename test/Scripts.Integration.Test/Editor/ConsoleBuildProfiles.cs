using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configures console platform build profiles via reflection.
/// The Unity Editor API for these settings is either deprecated or requires platform modules that
/// may not be installed. We access the internal BuildProfile assets directly instead.
/// </summary>
internal static class ConsoleBuildProfiles
{
    internal static void SetXboxSubtargetToMaster()
    {
        // The actual editor API to set this has been deprecated: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/XboxBuildSubtarget.html
        // Modifying the build profiles and build setting assets on disk does not work. Some of the properties are
        // stored inside a binary. Instead we're setting the properties via reflection and then saving the asset.
        var buildProfileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.CoreModule");
        if (buildProfileType == null)
        {
            return;
        }

        foreach (var profile in Resources.FindObjectsOfTypeAll(buildProfileType))
        {
            // BuildTarget.GameCoreXboxSeries = 42, BuildTarget.GameCoreXboxOne = 43.
            var buildTarget = new SerializedObject(profile).FindProperty("m_BuildTarget")?.intValue ?? -1;
            if (buildTarget != 42 && buildTarget != 43)
                continue;

            var platformSettings = buildProfileType
                .GetProperty("platformBuildProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(profile);
            var settingsData = platformSettings?.GetType()
                .GetField("m_settingsData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(platformSettings);

            GetFieldInHierarchy(settingsData?.GetType(), "buildSubtarget")?.SetValue(settingsData, 1); // 1 = Master
            GetFieldInHierarchy(platformSettings?.GetType(), "m_Development")?.SetValue(platformSettings, false);
            GetFieldInHierarchy(settingsData?.GetType(), "deploymentMethod")?.SetValue(settingsData, 2); // 2 = Package

            EditorUtility.SetDirty(profile);
            Debug.Log($"Builder: Xbox Build Profile (BuildTarget {buildTarget}) set to Master, deploy method set to Package");
        }

        AssetDatabase.SaveAssets();
    }

    internal static void SetPS5BuildTypeToPackage()
    {
        var buildProfileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.CoreModule");
        if (buildProfileType == null)
        {
            return;
        }

        foreach (var profile in Resources.FindObjectsOfTypeAll(buildProfileType))
        {
            // BuildTarget.PS5 = 44.
            var buildTarget = new SerializedObject(profile).FindProperty("m_BuildTarget")?.intValue ?? -1;
            if (buildTarget != 44)
                continue;

            var platformSettings = buildProfileType
                .GetProperty("platformBuildProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(profile);

            GetFieldInHierarchy(platformSettings?.GetType(), "m_Development")?.SetValue(platformSettings, false);
            GetFieldInHierarchy(platformSettings?.GetType(), "m_BuildSubtarget")?.SetValue(platformSettings, 1); // 1 = Package

            EditorUtility.SetDirty(profile);
            Debug.Log("Builder: PS5 Build Profile set to Package");
        }

        AssetDatabase.SaveAssets();
    }

    internal static void SetSwitchCreateNspRomFile()
    {
        var buildProfileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.CoreModule");
        if (buildProfileType == null)
        {
            return;
        }

        foreach (var profile in Resources.FindObjectsOfTypeAll(buildProfileType))
        {
            // BuildTarget.Switch = 38.
            var buildTarget = new SerializedObject(profile).FindProperty("m_BuildTarget")?.intValue ?? -1;
            if (buildTarget != 38)
                continue;

            var platformSettings = buildProfileType
                .GetProperty("platformBuildProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(profile);

            GetFieldInHierarchy(platformSettings?.GetType(), "m_Development")?.SetValue(platformSettings, false);
            GetFieldInHierarchy(platformSettings?.GetType(), "m_SwitchCreateRomFile")?.SetValue(platformSettings, true);

            EditorUtility.SetDirty(profile);
            Debug.Log("Builder: Switch Build Profile set to Create NSP ROM File");
        }

        AssetDatabase.SaveAssets();
    }

    private static FieldInfo GetFieldInHierarchy(Type type, string fieldName)
    {
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field != null)
                return field;
            type = type.BaseType;
        }
        return null;
    }
}
