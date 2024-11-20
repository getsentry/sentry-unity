using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class OptionsConfigurationTab
{
    public static void Display(ScriptableSentryUnityOptions options)
    {
        GUILayout.Label("Scriptable Options Configuration", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Clicking the 'New' button will prompt you with selecting a location for " +
                                "your custom 'SentryConfiguration' script and automatically " +
                                "create a new asset instance.", MessageType.Info);

        EditorGUILayout.Space();
    
        if (options.RuntimeOptionsConfiguration != null || options.BuildTimeOptionsConfiguration != null)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox(
                "The Runtime/BuildTime scriptable objects have been deprecated and will be removed in a future version." +
                "\nPlease use the 'Option Config Script' below." +
                "\nInstead of implementing your configuration in two places you can control the options via precompile directives.",
                MessageType.Warning);

            if(options.RuntimeOptionsConfiguration != null)
            {
                options.RuntimeOptionsConfiguration = OptionsConfigurationItem.Display(
                    options.RuntimeOptionsConfiguration,
                    "Runtime Config Script",
                    "SentryRuntimeConfiguration",
                    "DEPRECATED: A scriptable object that inherits from 'ScriptableOptionsConfiguration' " +
                    "and allows you to programmatically modify Sentry options.");
            }

            if(options.BuildTimeOptionsConfiguration != null)
            {
                options.BuildTimeOptionsConfiguration = OptionsConfigurationItem.Display(
                    options.BuildTimeOptionsConfiguration,
                    "Build Time Config Script",
                    "SentryBuildTimeConfiguration",
                    "DEPRECATED: A scriptable object that inherits from 'ScriptableOptionsConfiguration' " +
                    "and allows you to programmatically modify Sentry options.");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }
        
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.HelpBox("The 'Option Config Script' allows you to programmatically configure and " +
                                "modify the options used by the Sentry SDK.", MessageType.Info);

        options.OptionsConfiguration = OptionsConfigurationItem.Display(
            options.OptionsConfiguration,
            "Option Config Script",
            "SentryOptionConfiguration",
            "A scriptable object that inherits from 'SentryOptionsConfiguration' " +
            "and allows you to programmatically modify Sentry options."
        );

        EditorGUILayout.EndVertical();
    }
}
