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

        EditorGUILayout.BeginVertical("box");

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

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Deprecated and will be removed in a future version.\nPlease use the 'Option Config Script' below instead.", MessageType.Warning);

        options.RuntimeOptionsConfiguration = OptionsConfigurationItem.Display(
            options.RuntimeOptionsConfiguration,
            "Runtime Config Script",
            "SentryRuntimeConfiguration",
            "DEPRECATED: A scriptable object that inherits from 'ScriptableOptionsConfiguration' " +
            "and allows you to programmatically modify Sentry options."
        );

        options.BuildTimeOptionsConfiguration = OptionsConfigurationItem.Display(
            options.BuildTimeOptionsConfiguration,
            "Build Time Config Script",
            "SentryBuildTimeConfiguration",
            "DEPRECATED: A scriptable object that inherits from 'ScriptableOptionsConfiguration' " +
            "and allows you to programmatically modify Sentry options."
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

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
