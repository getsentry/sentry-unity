using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class AdvancedTab
    {
        internal static void Display(ScriptableSentryUnityOptions options)
        {
            options.Debug = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Enable Debug Output", "Whether the Sentry SDK should print its " +
                                                      "diagnostic logs to the console."),
                options.Debug);

            options.DebugOnlyInEditor = EditorGUILayout.Toggle(
                new GUIContent("Only In Editor", "Only print logs when in the editor. Development " +
                                                 "builds of the player will not include Sentry's SDK diagnostics."),
                options.DebugOnlyInEditor);

            options.DiagnosticLevel = (SentryLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Verbosity Level", "The minimum level allowed to be printed to the console. " +
                                                  "Log messages with a level below this level are dropped."),
                options.DiagnosticLevel);

            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            options.AutoSessionTracking = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Auto Session Tracking", "Whether the SDK should start and end sessions " +
                                                        "automatically. If the timeout is reached the old session will" +
                                                        "be ended and a new one started."),
                options.AutoSessionTracking);

            options.AutoSessionTrackingInterval = EditorGUILayout.IntField(
                new GUIContent("Session Timeout [ms]", "The duration of time a session can stay paused " +
                                                       "(i.e. the application has been put in the background) before " +
                                                       "it is considered ended."),
                options.AutoSessionTrackingInterval);
            options.AutoSessionTrackingInterval = Mathf.Max(0, options.AutoSessionTrackingInterval);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            GUILayout.Label("Native Support", EditorStyles.boldLabel);

            options.IosNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("iOS Native Support", "Whether to enable Native iOS support to capture" +
                                                     "errors written in languages such as Objective-C, Swift, C and C++."),
                options.IosNativeSupportEnabled);

            options.AndroidNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Android Native Support", "Whether to enable Native Android support to " +
                                                         "capture errors written in languages such as Java, Kotlin, C and C++."),
                options.AndroidNativeSupportEnabled);

            options.WindowsNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Windows Native Support", "Whether to enable native crashes support on Windows."),
                options.WindowsNativeSupportEnabled);

            options.MacosNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("macOS Native Support", "Whether to enable native crashes support on macOS."),
                options.MacosNativeSupportEnabled);
        }


    }
}
