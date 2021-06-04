using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryGettingStartedWindow : EditorWindow
    {
        private string? _dsn;

        [MenuItem("Tools/Sentry/Getting Started")]
        public static SentryGettingStartedWindow OpenWindow()
            => (SentryGettingStartedWindow)GetWindow(
                typeof(SentryGettingStartedWindow), true, "Sentry Getting Started Window");

        private void OnGUI()
        {
            EditorGUILayout.Space();

            GUILayout.Label("All that you need to get started is to provide a DSN.");

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            _dsn = EditorGUILayout.TextField(
                new GUIContent("DSN", "The URL to your project inside Sentry. Get yours in Sentry, Project Settings."),
                _dsn);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            if (GUILayout.Button("Enable"))
            {
                this.Close();
            }
        }
    }
}
