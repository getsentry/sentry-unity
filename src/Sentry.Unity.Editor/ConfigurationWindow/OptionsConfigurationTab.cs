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
            "SentryRuntimeConfiguration",
            "A scriptable object that inherits from 'ScriptableOptionsConfiguration' " +
            "and allows you to programmatically modify Sentry options."
        );

        options.BuildTimeOptionsConfiguration = OptionsConfigurationItem.Display(
            options.BuildTimeOptionsConfiguration,
            "Build Time Configuration Script",
            "SentryBuildTimeConfiguration",
            "A scriptable object that inherits from 'ScriptableOptionsConfiguration' " +
            "and allows you to programmatically modify Sentry options."
        );
    }
}
