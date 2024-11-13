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
                CreateDeprecatedConfigurationScript<T>(fieldName, scriptName);
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

    private static void CreateDeprecatedConfigurationScript<T>(string fieldName, string scriptName)
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

        var template = new StringBuilder();
        template.AppendLine("using UnityEngine;");
        template.AppendLine("using Sentry.Unity;");
        template.AppendLine();
        template.AppendFormat("[CreateAssetMenu(fileName = \"{0}\", menuName = \"Sentry/{1}\", order = 999)]\n", SentryAssetPath(scriptName), scriptName);
        template.AppendFormat("public class {0} : {1}\n", scriptName, typeof(T).FullName);
        template.AppendLine("{");

        if (typeof(T) == typeof(SentryBuildTimeOptionsConfiguration))
        {
            template.AppendLine("    /// Called during app build. Changes made here will affect build-time processing, symbol upload, etc.");
            template.AppendLine("    /// Additionally, because iOS, macOS and Android native error handling is configured at build time,");
            template.AppendLine("    /// you can make changes to these options here.");
        }
        else
        {
            template.AppendLine("    /// Called at the player startup by SentryInitialization.");
            template.AppendLine("    /// You can alter configuration for the C# error handling and also");
            template.AppendLine("    /// native error handling in platforms **other** than iOS, macOS and Android.");
        }

        template.AppendLine("    /// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration");
        template.AppendFormat("    public override void Configure(SentryUnityOptions options{0})\n",
            typeof(T) == typeof(SentryBuildTimeOptionsConfiguration) ? ", SentryCliOptions cliOptions" : "");
        template.AppendLine("    {");
        if (typeof(T) != typeof(SentryBuildTimeOptionsConfiguration))
        {
            template.AppendLine("        // Note that changes to the options here will **not** affect iOS, macOS and Android events. (i.e. environment and release)");
            template.AppendLine("        // Take a look at `SentryBuildTimeOptionsConfiguration` instead.");
        }
        template.AppendLine("        // TODO implement");
        template.AppendLine("    }");
        template.AppendLine("}");

        File.WriteAllText(scriptPath, template.ToString().Replace("\r\n", "\n"));

        // The created script has to be compiled and the scriptable object can't immediately be instantiated.
        // So instead we work around this by setting a 'CreateScriptableObjectFlag' flag in the EditorPrefs to
        // trigger the creation after the scripts reloaded.
        EditorPrefs.SetBool(CreateScriptableObjectFlag, true);
        EditorPrefs.SetString(ScriptNameKey, scriptName);

        AssetDatabase.ImportAsset(scriptPath);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scriptPath);
    }

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
        AssetDatabase.CreateAsset(optionsConfigurationObject, SentryAssetPath(scriptNameWithoutExtension));
        AssetDatabase.Refresh();

        var options = EditorWindow.GetWindow<SentryWindow>().Options;
        var cliOptions = EditorWindow.GetWindow<SentryWindow>().CliOptions;

        switch (optionsConfigurationObject)
        {
            case SentryRuntimeOptionsConfiguration runtimeConfiguration:
                options.RuntimeOptionsConfiguration ??= runtimeConfiguration; // Don't overwrite if already set
                break;
            case SentryBuildTimeOptionsConfiguration buildTimeConfiguration:
                options.BuildTimeOptionsConfiguration ??= buildTimeConfiguration; // Don't overwrite if already set
                break;
            case SentryOptionsConfiguration configuration:
                options.OptionsConfiguration ??= configuration; // Don't overwrite if already set
                break;
            case SentryCliOptionsConfiguration cliConfiguration:
                cliOptions.CliOptionsConfiguration ??= cliConfiguration; // Don't overwrite if already set
                break;
        }
    }
}
