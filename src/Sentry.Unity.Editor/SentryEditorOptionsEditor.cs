using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    [CustomEditor(typeof(SentryEditorOptions))]
    public class SentryEditorOptionsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target is not SentryEditorOptions cliOptions)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.Toggle("Enable Symbols Upload", cliOptions.UploadSymbols);
            EditorGUILayout.Toggle("Enable Dev Symbols Upload", cliOptions.UploadDevelopmentSymbols);
            EditorGUILayout.TextField("Auth-Token", cliOptions.Auth);
            EditorGUILayout.TextField("Org-Slug", cliOptions.Organization);
            EditorGUILayout.TextField("Project Name", cliOptions.Project);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.Toggle("Add Sentry to Windows Player", cliOptions.AddSentryToWindowsPlayer);
            EditorGUILayout.TextField("MSBuild Path", cliOptions.MSBuildPath);
            EditorGUILayout.TextField("VSWhere Path", cliOptions.VSWherePath);

            EditorGUI.EndDisabledGroup();
        }
    }
}
