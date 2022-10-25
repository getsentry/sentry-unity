using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class OptionsConfigurationTab
    {
        public static void Display(ScriptableSentryUnityOptions options)
        {
            GUILayout.Label("Scriptable Options Configuration", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("The scriptable options configuration allows you to programmatically " +
                                    "modify the Sentry options at runtime, right before the SDK gets initialized." +
                                    "This allows you to override configuration otherwise unavailable from the " +
                                    "Editor UI, e.g. set a custom BeforeSend callback.",
                                    MessageType.Info);

            EditorGUILayout.HelpBox("Clicking the 'New' button will prompt you with selecting a location for " +
                                    "your custom 'ScriptableSentryConfiguration' script and automatically " +
                                    "create a new instance at 'Assets/Resources/Sentry/'",
                                    // TODO other platforms
                                    // "Because Sentry Unity integration includes both managed C# Unity SDK and a " +
                                    // "platform specific one, you can specify the respective overrides separately.\n\n" +
                                    MessageType.Info);

            EditorGUILayout.Space();

            OptionsConfigurationDotNet.Display(options);
        }
    }

    internal static class OptionsConfigurationDotNet
    {
        private const string CreateScriptableObjectFlag = "CreateScriptableOptionsObject";
        private const string ScriptNameKey = "ScriptableOptionsName";

        public static void Display(ScriptableSentryUnityOptions options)
        {
            GUILayout.BeginHorizontal();
            options.OptionsConfiguration = EditorGUILayout.ObjectField(
                    new GUIContent(".NET (C#)", "A scriptable object that inherits from " +
                                                            "'ScriptableOptionsConfiguration' and allows you to " +
                                                            "programmatically modify Sentry options."),
                    options.OptionsConfiguration, typeof(ScriptableOptionsConfiguration), false)
                as ScriptableOptionsConfiguration;
            if (GUILayout.Button("New", GUILayout.ExpandWidth(false)))
            {
                CreateScript();
            }
            GUILayout.EndHorizontal();
        }

        internal static void CreateScript()
        {
            var scriptPath = EditorUtility.SaveFilePanel("Sentry Options Configuration", "Assets", "SentryOptionsConfiguration", "cs");
            if (String.IsNullOrEmpty(scriptPath))
            {
                return;
            }

            if (scriptPath.StartsWith(Application.dataPath))
            {
                // AssetDatabase prefers a relative path
                scriptPath = "Assets" + scriptPath.Substring(Application.dataPath.Length);
            }

            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);

            File.WriteAllText(scriptPath, $@"using System;
using Sentry.Unity;
using UnityEngine;

/// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
[CreateAssetMenu(fileName = ""Assets/Resources/Sentry/{scriptName}.cs"", menuName = ""Sentry/{scriptName}"", order = 999)]
public class {scriptName} : ScriptableOptionsConfiguration
{{
    /// See base class for documentation.
    public override void ConfigureAtBuild(SentryUnityOptions options)
    {{
        // TODO implement
        throw new NotImplementedException();
    }}

    /// See base class for documentation.
    public override void ConfigureAtRuntime(SentryUnityOptions options)
    {{
        // TODO implement
        throw new NotImplementedException();
    }}
}}
");

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

        internal static void SetScript(String scriptName)
        {
            var optionsConfigurationObject = ScriptableObject.CreateInstance(scriptName);
            AssetDatabase.CreateAsset(optionsConfigurationObject, $"Assets/Resources/Sentry/{scriptName}.asset");
            AssetDatabase.Refresh();

            // Don't overwrite already set OptionsConfiguration
            var options = SentryWindow.Instance!.Options;
            if (options.OptionsConfiguration == null)
            {
                options.OptionsConfiguration = (ScriptableOptionsConfiguration)optionsConfigurationObject;
            }
        }
    }
}
