using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class DebugSymbolsTab
{
    internal static void Display(SentryCliOptions cliOptions)
    {
        cliOptions.UploadSymbols = EditorGUILayout.BeginToggleGroup(
            new GUIContent("Upload Symbols", "Whether debug symbols should be uploaded automatically " +
                                             "on release builds."),
            cliOptions.UploadSymbols);
        EditorGUI.indentLevel++;

        cliOptions.UploadDevelopmentSymbols = EditorGUILayout.Toggle(
            new GUIContent("Upload Dev Symbols", "Whether debug symbols should be uploaded automatically " +
                                                 "on development builds."),
            cliOptions.UploadDevelopmentSymbols);

        cliOptions.UploadSources = EditorGUILayout.Toggle(
            new GUIContent("Upload Sources", "Whether your source code should be uploaded to Sentry, so the stack trace in Sentry has the relevant code next to it."),
            cliOptions.UploadSources);

        cliOptions.IgnoreCliErrors = EditorGUILayout.Toggle(
            new GUIContent("Ignore CLI Errors", "Whether to ignore the Sentry CLI errors during the build. If this is ON, the errors will be logged, but the build will succeed."),
            cliOptions.IgnoreCliErrors);

        EditorGUILayout.EndToggleGroup();

        cliOptions.Auth = EditorGUILayout.TextField(
            new GUIContent(
                "Auth Token",
                cliOptions.UploadSymbols && string.IsNullOrWhiteSpace(cliOptions.Auth) ? SentryWindow.ErrorIcon : null,
                "The authorization token from your user settings in Sentry"),
            cliOptions.Auth);

        cliOptions.Organization = EditorGUILayout.TextField(
                new GUIContent(
                    "Org Slug",
                    "The organization slug in Sentry"),
                cliOptions.Organization);

        cliOptions.Project = EditorGUILayout.TextField(
            new GUIContent(
                "Project Name",
                cliOptions.UploadSymbols && string.IsNullOrWhiteSpace(cliOptions.Project) ? SentryWindow.ErrorIcon : null,
                "The project name in Sentry"),
            cliOptions.Project);

        EditorGUILayout.Space();

        cliOptions.CliOptionsConfiguration = OptionsConfigurationItem.Display(
            cliOptions.CliOptionsConfiguration,
            "CLI Config Script",
            "SentryCliConfiguration",
            "A scriptable object that inherits from 'SentryCliOptionsConfiguration'." +
            "It allows you to programmatically modify the options used during debug symbols upload."
        );

        EditorGUI.indentLevel--;
    }
}
