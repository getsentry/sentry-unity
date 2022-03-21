using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class DebugSymbolsTab
    {
        private static readonly ReorderableList SymbolList = new ReorderableList(null, typeof( string ))
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Additional Symbol Paths"),
            drawElementCallback = (rect, elementIdx, _, _) =>
            {
                rect = EditorGUI.PrefixLabel(rect, new GUIContent($"Element {elementIdx}"));

                var element = SymbolList!.list[elementIdx];
                var symbolList = EditorGUI.TextField(rect, (string)element);
                SymbolList.list[elementIdx] = symbolList;
            }
        };

        internal static void Display(SentryCliOptions cliOptions)
        {
            cliOptions.UploadSymbols = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Upload Symbols", "Whether debug symbols should be uploaded automatically " +
                                                 "on release builds."),
                cliOptions.UploadSymbols);

            cliOptions.UploadDevelopmentSymbols = EditorGUILayout.Toggle(
                new GUIContent("Upload Dev Symbols", "Whether debug symbols should be uploaded automatically " +
                                                     "on development builds."),
                cliOptions.UploadDevelopmentSymbols);

            EditorGUILayout.EndToggleGroup();

            cliOptions.Auth = EditorGUILayout.TextField(
                new GUIContent("Auth Token", "The authorization token from your user settings in Sentry"),
                cliOptions.Auth);

            cliOptions.Organization = EditorGUILayout.TextField(
                new GUIContent("Org Slug", "The organization slug in Sentry"),
                cliOptions.Organization);

            cliOptions.Project = EditorGUILayout.TextField(
                new GUIContent("Project Name", "The project name in Sentry"),
                cliOptions.Project);

            SymbolList.list = cliOptions.AdditionalSymbolPaths;
            SymbolList.DoLayoutList();
        }
    }
}
