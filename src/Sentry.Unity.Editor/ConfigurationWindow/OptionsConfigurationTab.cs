using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class OptionsConfigurationTab
{
    public static void Display(ScriptableSentryUnityOptions options)
    {
        GUILayout.Label("Scriptable Options Configuration", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "The scriptable options configuration allows you to programmatically modify Sentry options." +
            "\n" +
            "\n" +
            "You can use the 'Runtime Configuration Script' to modify options just before Sentry SDK gets " +
            "initialized. This allows you to access options and functionality otherwise unavailable from the " +
            "Editor UI, e.g. set a custom BeforeSend callback." +
            "\n" +
            "\n" +
            "Use the 'Build Time Configuration Script' in case you need to change build-time behavior, " +
            "e.g. specify custom Sentry-CLI options or change settings for native SDKs that start before the " +
            "managed layer does (such as Android, iOS, macOS).",
            MessageType.Info);

        EditorGUILayout.HelpBox("Clicking the 'New' button will prompt you with selecting a location for " +
                                "your custom 'SentryConfiguration' script and automatically " +
                                "create a new asset instance.", MessageType.Info);

        EditorGUILayout.Space();

        options.RuntimeOptionsConfiguration = OptionsConfigurationItem.Display(
            options.RuntimeOptionsConfiguration,
            "Runtime Configuration Script",
            "SentryRuntimeConfiguration"
        );

        options.BuildTimeOptionsConfiguration = OptionsConfigurationItem.Display(
            options.BuildTimeOptionsConfiguration,
            "Build Time Configuration Script",
            "SentryBuildTimeConfiguration"
        );
    }
}

internal static class OptionsConfigurationItem
{
    private const string CreateScriptableObjectFlag = "Sentry/CreateScriptableOptionsObject";
    private const string ScriptNameKey = "Sentry/ScriptableOptionsScript";

    public static T? Display<T>(T? value, string fieldName, string scriptName) where T : ScriptableObject
    {
        GUILayout.BeginHorizontal();
        var result = EditorGUILayout.ObjectField(
            new GUIContent(fieldName, "A scriptable object that inherits from 'ScriptableOptionsConfiguration' " +
                                      "and allows you to programmatically modify Sentry options."),
            value,
            typeof(T),
            false
        ) as T;
        if (GUILayout.Button("New", GUILayout.ExpandWidth(false)))
        {
            CreateScript<T>(fieldName, scriptName);
        }
        GUILayout.EndHorizontal();
        return result;
    }

    private static string SentryAssetPath(string scriptName) => $"Assets/Resources/Sentry/{scriptName}.asset";

    private static void CreateScript<T>(string fieldName, string scriptName)
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
        var isEditorScript = optionsConfigurationObject is SentryBuildTimeOptionsConfiguration;
        AssetDatabase.CreateAsset(optionsConfigurationObject, SentryAssetPath(scriptNameWithoutExtension));
        AssetDatabase.Refresh();

        var options = EditorWindow.GetWindow<SentryWindow>().Options;

        if (isEditorScript)
        {
            // Don't overwrite already set OptionsConfiguration
            options.BuildTimeOptionsConfiguration ??= optionsConfigurationObject as SentryBuildTimeOptionsConfiguration;
        }
        else
        {
            // Don't overwrite already set OptionsConfiguration
            options.RuntimeOptionsConfiguration ??= optionsConfigurationObject as SentryRuntimeOptionsConfiguration;
        }
    }
}