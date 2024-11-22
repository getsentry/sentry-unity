using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class OptionsConfigurationItem
{
    private const string CreateScriptableObjectFlag = "Sentry/CreateScriptableOptionsObject";
    private const string ScriptNameKey = "Sentry/ScriptableOptionsScript";

    public static T? Display<T>(T? value, string fieldName, string scriptName, string toolTip) where T : ScriptableObject
    {
        EditorGUILayout.BeginHorizontal();

        var result = EditorGUILayout.ObjectField(
            new GUIContent(fieldName, toolTip),
            value,
            typeof(T),
            false
        ) as T;

        if (GUILayout.Button("New", GUILayout.ExpandWidth(false)))
        {
            var t = typeof(T);
            if (t == typeof(SentryRuntimeOptionsConfiguration) || t == typeof(SentryBuildTimeOptionsConfiguration))
            {
                Debug.LogWarning("Runtime/BuildTime scriptable objects have been deprecated and will be removed in a future version." +
                                 "Please use the 'Option Config Script' below.");
            }
            else if (t == typeof(SentryOptionsConfiguration))
            {
                CreateConfigurationScript(fieldName, SentryOptionsConfiguration.Template, scriptName);
            }
            else if (t == typeof(SentryCliOptionsConfiguration))
            {
                CreateConfigurationScript(fieldName, SentryCliOptionsConfiguration.Template, scriptName);
            }
            else
            {
                throw new Exception("Attempting to create a new instance of unsupported type " + typeof(T).FullName);
            }
        }

        EditorGUILayout.EndHorizontal();
        return result;
    }

    private static string SentryAssetPath(string scriptName) => $"Assets/Resources/Sentry/{scriptName}.asset";

    private static void CreateConfigurationScript(string fieldName, string template, string scriptName)
    {
        const string directory = "Assets/Scripts";
        if (!AssetDatabase.IsValidFolder(directory))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(directory), Path.GetFileName(directory));
        }

        var scriptPath = EditorUtility.SaveFilePanel(fieldName, directory, scriptName, "cs");
        if (string.IsNullOrEmpty(scriptPath))
        {
            return;
        }

        if (scriptPath.StartsWith(Application.dataPath))
        {
            // AssetDatabase prefers a relative path
            scriptPath = "Assets" + scriptPath.Substring(Application.dataPath.Length);
        }

        scriptName = Path.GetFileNameWithoutExtension(scriptPath);
        var script = template.Replace("{{ScriptName}}", scriptName);

        File.WriteAllText(scriptPath, script);

        // The created script has to be compiled and the scriptable object can't immediately be instantiated.
        // So instead we work around this by setting a 'CreateScriptableObjectFlag' flag in the EditorPrefs to
        // trigger the creation after the scripts reloaded.
        EditorPrefs.SetBool(CreateScriptableObjectFlag, true);
        EditorPrefs.SetString(ScriptNameKey, scriptName);

        AssetDatabase.ImportAsset(scriptPath);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scriptPath);
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (!EditorPrefs.GetBool(CreateScriptableObjectFlag))
        {
            return;
        }

        var scriptName = EditorPrefs.GetString(ScriptNameKey);
        EditorPrefs.DeleteKey(CreateScriptableObjectFlag);
        EditorPrefs.DeleteKey(ScriptNameKey);

        SetScript(scriptName);
    }

    internal static void SetScript(string scriptNameWithoutExtension)
    {
        var optionsConfigurationObject = ScriptableObject.CreateInstance(scriptNameWithoutExtension);

        var options = EditorWindow.GetWindow<SentryWindow>().Options;
        var cliOptions = EditorWindow.GetWindow<SentryWindow>().CliOptions;

        switch (optionsConfigurationObject)
        {
            case SentryRuntimeOptionsConfiguration runtimeConfiguration:
                AssetDatabase.CreateAsset(optionsConfigurationObject, SentryAssetPath(scriptNameWithoutExtension));
                AssetDatabase.Refresh();
                options.RuntimeOptionsConfiguration ??= runtimeConfiguration; // Don't overwrite if already set
                break;
            case SentryBuildTimeOptionsConfiguration buildTimeConfiguration:
                AssetDatabase.CreateAsset(optionsConfigurationObject, SentryAssetPath(scriptNameWithoutExtension));
                AssetDatabase.Refresh();
                options.BuildTimeOptionsConfiguration ??= buildTimeConfiguration; // Don't overwrite if already set
                break;
            case SentryOptionsConfiguration configuration:
                AssetDatabase.CreateAsset(optionsConfigurationObject, SentryOptionsConfiguration.GetAssetPath(scriptNameWithoutExtension));
                AssetDatabase.Refresh();
                options.OptionsConfiguration ??= configuration; // Don't overwrite if already set
                break;
            case SentryCliOptionsConfiguration cliConfiguration:
                AssetDatabase.CreateAsset(optionsConfigurationObject, SentryCliOptionsConfiguration.GetAssetPath(scriptNameWithoutExtension));
                AssetDatabase.Refresh();
                cliOptions.CliOptionsConfiguration ??= cliConfiguration; // Don't overwrite if already set
                break;
        }
    }
}
