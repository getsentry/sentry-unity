using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class EditorOptions
    {
        internal static void Display(SentryEditorOptions editorOptions)
        {
            editorOptions.UploadSymbols = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Upload Symbols", "Whether debug symbols should be uploaded automatically " +
                                                 "on release builds."),
                editorOptions.UploadSymbols);

            editorOptions.UploadDevelopmentSymbols = EditorGUILayout.Toggle(
                new GUIContent("Upload Dev Symbols", "Whether debug symbols should be uploaded automatically " +
                                                     "on development builds."),
                editorOptions.UploadDevelopmentSymbols);

            EditorGUILayout.EndToggleGroup();

            editorOptions.Auth = EditorGUILayout.TextField(
                new GUIContent("Auth Token", "The authorization token from your user settings in Sentry"),
                editorOptions.Auth);

            editorOptions.Organization = EditorGUILayout.TextField(
                new GUIContent("Org Slug", "The organization slug in Sentry"),
                editorOptions.Organization);

            editorOptions.Project = EditorGUILayout.TextField(
                new GUIContent("Project Name", "The project name in Sentry"),
                editorOptions.Project);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            editorOptions.AddSentryToWindowsPlayer = EditorGUILayout.Toggle(
                new GUIContent("Add Sentry to Windows Player", "If enabled the SDK will " +
                                                "compile the Windows Player from source and add Sentry to it."),
                editorOptions.AddSentryToWindowsPlayer);

            editorOptions.MSBuildPath = EditorGUILayout.TextField(
                new GUIContent("MSBuild Path", "The path to MSBuild, if left empty the SDK will " +
                                               "try to locate it."),
                editorOptions.MSBuildPath);

            editorOptions.VSWherePath = EditorGUILayout.TextField(
                new GUIContent("VSWhere Path", "The path to VSWhere used to locate MSBuild. If " +
                                               "left empty the SDK will try to locate it."),
                editorOptions.VSWherePath);
        }
    }
}
