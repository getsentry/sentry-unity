using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class CoreTab
    {
        internal static void Display(ScriptableSentryUnityOptions options)
        {
            {
                GUILayout.Label("Base Options", EditorStyles.boldLabel);

                options.Dsn = EditorGUILayout.TextField(
                    new GUIContent("DSN", "The URL to your Sentry project. " +
                                          "Get yours on sentry.io -> Project Settings."),
                    options.Dsn)?.Trim();

                if (string.IsNullOrWhiteSpace(options.Dsn))
                {
                    EditorGUILayout.HelpBox("The SDK requires a DSN.", MessageType.Error);
                }

                options.CaptureInEditor = EditorGUILayout.Toggle(
                    new GUIContent("Capture In Editor", "Capture errors while running in the Editor."),
                    options.CaptureInEditor);

                options.EnableLogDebouncing = EditorGUILayout.Toggle(
                    new GUIContent("Enable Log Debouncing", "The SDK debounces log messages of the same type if " +
                                                            "they are more frequent than once per second."),
                    options.EnableLogDebouncing);
            }

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

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
            }

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            {
                options.EnableLogDebouncing = EditorGUILayout.BeginToggleGroup(
                    new GUIContent("Enable Log Debouncing", "The SDK debounces log messages of the " +
                                                            "same type if they are more frequent than once per second."),
                    options.EnableLogDebouncing);

                options.DebounceTimeLog = EditorGUILayout.IntField(
                        new GUIContent("Log Debounce [ms]", "The time that has to pass between events of " +
                                                            "LogType.Log before the SDK sends it again."),
                        options.DebounceTimeLog);
                options.DebounceTimeLog = Math.Max(0, options.DebounceTimeLog);

                options.DebounceTimeWarning = EditorGUILayout.IntField(
                    new GUIContent("Warning Debounce [ms]", "The time that has to pass between events of " +
                                                            "LogType.Warning before the SDK sends it again."),
                    options.DebounceTimeWarning);
                options.DebounceTimeWarning = Math.Max(0, options.DebounceTimeWarning);

                options.DebounceTimeError = EditorGUILayout.IntField(
                    new GUIContent("Error Debounce [ms]", "The time that has to pass between events of " +
                                                            "LogType.Assert, LogType.Exception and LogType.Error before " +
                                                            "the SDK sends it again."),
                    options.DebounceTimeError);
                options.DebounceTimeError = Math.Max(0, options.DebounceTimeError);

                EditorGUILayout.EndToggleGroup();
            }

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            {
                GUILayout.Label("Tracing - Performance Monitoring", EditorStyles.boldLabel);

                options.TracesSampleRate = EditorGUILayout.Slider(
                    new GUIContent("Traces Sample Rate", "Indicates the percentage of transactions that are " +
                                                         "captured. Setting this to 0 discards all trace data. " +
                                                         "Setting this to 1.0 captures all."),
                    (float)options.TracesSampleRate, 0.0f, 1.0f);

                EditorGUI.BeginDisabledGroup(options.TracesSampleRate <= 0);

                options.AutoStartupTraces = EditorGUILayout.Toggle(
                    new GUIContent("Auto Startup Traces ", "Whether the SDK should automatically create " +
                                                           "traces during startup. This integration is currently " +
                                                           "unavailable on WebGL."),
                    options.AutoStartupTraces);

                options.AutoSceneLoadTraces = EditorGUILayout.Toggle(
                    new GUIContent("Auto Scene Traces ", "Whether the SDK should automatically create traces " +
                                                         "during scene loading. Requires Unity 2020.3 or newer."),
                    options.AutoSceneLoadTraces);

                EditorGUILayout.Space();

                GUILayout.Label("Auto Instrumentation - Experimental", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox("The SDK will modify the compiled assembly during a post build step " +
                                        "to create transaction and spans automatically.", MessageType.Info);

                options.AutoAwakeTraces = EditorGUILayout.Toggle(
                    new GUIContent("Awake Calls", "Whether the SDK automatically captures all instances " +
                                                  "of Awake as Spans."),
                    options.AutoAwakeTraces);

                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
