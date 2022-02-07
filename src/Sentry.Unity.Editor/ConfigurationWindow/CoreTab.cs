using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class CoreTab
    {
        internal static void Display(ScriptableSentryUnityOptions options)
        {
            GUILayout.Label("Base Options", EditorStyles.boldLabel);

            options.Dsn = EditorGUILayout.TextField(
                new GUIContent("DSN", "The URL to your Sentry project. " +
                                      "Get yours on sentry.io -> Project Settings."),
                options.Dsn)?.Trim();

            options.CaptureInEditor = EditorGUILayout.Toggle(
                new GUIContent("Capture In Editor", "Capture errors while running in the Editor."),
                options.CaptureInEditor);

            options.EnableLogDebouncing = EditorGUILayout.Toggle(
                new GUIContent("Enable Log Debouncing", "The SDK debounces log messages of the same type if " +
                                                        "they are more frequent than once per second."),
                options.EnableLogDebouncing);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            GUILayout.Label("Tracing - Performance Monitoring", EditorStyles.boldLabel);

            options.TracesSampleRate = EditorGUILayout.Slider(
                new GUIContent("Traces Sample Rate", "Indicates the percentage of transactions that are " +
                                                     "captured. Setting this to 0 discards all trace data. " +
                                                     "Setting this to 1.0 captures all."),
                (float)options.TracesSampleRate, 0.0f, 1.0f);
        }
    }
}
