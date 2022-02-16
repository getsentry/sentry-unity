using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class OptionsConfigurationTab
    {
        private const string CreateScriptableObjectFlag = "CreateScriptableOptionsObject";
        private const string ScriptNameKey = "ScriptableOptionsName";

        public static void Display(ScriptableSentryUnityOptions options)
        {
            GUILayout.Label("Programmatic Options Configuration", EditorStyles.boldLabel);

            options.OptionsConfiguration = EditorGUILayout.ObjectField(
                    new GUIContent("Options Configuration", "A scriptable object that inherits from " +
                                                            "'ScriptableOptionsConfiguration' that allows you to " +
                                                            "programmatically modify Sentry options i.e. implement " +
                                                            "the 'BeforeSend' callback."),
                    options.OptionsConfiguration, typeof(ScriptableOptionsConfiguration), false)
                as ScriptableOptionsConfiguration;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("The options configuration allows you to programmatically modify " +
                                    "the Sentry options object during runtime initialization of the SDK. " +
                                    "Clicking the button below will create a scriptable object template at your " +
                                    "targeted location and create an instance at 'Assets/Resources/Sentry/'.", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Create options configuration"))
            {
                CreateOptionsConfigurationScript();
            }
        }

        internal static void CreateOptionsConfigurationScript()
        {
            var scriptPath = EditorUtility.SaveFilePanel("Sentry Options Configuration", "Assets", "SentryOptionsConfiguration", "cs");
            if(String.IsNullOrEmpty(scriptPath))
            {
                return;
            }

            if (scriptPath.StartsWith(Application.dataPath))
            {
                // AssetDatabase prefers a relative path
                scriptPath = "Assets" + scriptPath.Substring(Application.dataPath.Length);
            }

            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);

            File.WriteAllText(scriptPath, $@"using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = ""Assets/Resources/Sentry/{scriptName}.cs"", menuName = ""Sentry/{scriptName}"", order = 999)]
public class {scriptName} : ScriptableOptionsConfiguration
{{
    // This method gets called when you instantiated the scriptable object and added it to the configuration window
    public override void Configure(SentryUnityOptions options)
    {{
        // NOTE: You have complete access to the options object here but changes to the options will not make it
        // to the native layer because the native layer is configured during build time.

        // Your code here
    }}
}}");

            // The created script has to be compiled and the scriptable object can't immediately be instantiated.
            // So instead we work around this by setting a 'ShouldCreateOptionsObject' flag in the EditorPrefs to
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

            var optionsConfigurationObject = ScriptableObject.CreateInstance(scriptName);
            AssetDatabase.CreateAsset(optionsConfigurationObject, $"Assets/Resources/Sentry/{scriptName}.asset");
            AssetDatabase.Refresh();

            // Don't overwrite already set OptionsConfiguration
            var options = SentryWindow.Instance.Options;
            if (options.OptionsConfiguration == null)
            {
                options.OptionsConfiguration = (ScriptableOptionsConfiguration)optionsConfigurationObject;
            }
        }
    }
}
