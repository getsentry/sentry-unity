using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class CoreTab
{
    internal static void Display(ScriptableSentryUnityOptions options)
    {
        {
            options.Dsn = EditorGUILayout.TextField(
                new GUIContent("DSN", "The URL to your Sentry project. " +
                                      "Get yours on sentry.io -> Project Settings."),
                options.Dsn)?.Trim();

            if (string.IsNullOrWhiteSpace(options.Dsn))
            {
                EditorGUILayout.HelpBox("The SDK requires a DSN.", MessageType.Error);
            }

            var sampleRate = EditorGUILayout.FloatField(
                new GUIContent("Event Sample Rate", "Indicates the percentage of events that are " +
                                                    "captured. Setting this to 0.1 captures 10% of events. " +
                                                    "Setting this to 1.0 captures all events." +
                                                    "\nThis affects only errors and logs, not performance " +
                                                    "(transactions) data. See TraceSampleRate for that."),
                options.SampleRate);
            options.SampleRate = Mathf.Clamp01(sampleRate);

            options.CaptureInEditor = EditorGUILayout.Toggle(
                new GUIContent("Capture In Editor", "Capture errors while running in the Editor."),
                options.CaptureInEditor);
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.Debug = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Enable Debug Output", "Whether the Sentry SDK should print its " +
                                                      "diagnostic logs to the console."),
                options.Debug);

            EditorGUI.indentLevel++;
            options.DebugOnlyInEditor = EditorGUILayout.Toggle(
                new GUIContent("Only In Editor", "Only print logs when in the editor. Development " +
                                                 "builds of the player will not include Sentry's SDK diagnostics."),
                options.DebugOnlyInEditor);

            options.DiagnosticLevel = (SentryLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Verbosity Level", "The minimum level allowed to be printed to the console. " +
                                                  "Log messages with a level below this level are dropped."),
                options.DiagnosticLevel);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            GUILayout.Label("Tracing - Performance Monitoring", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var sampleRate = EditorGUILayout.FloatField(
                new GUIContent("Traces Sample Rate", "Indicates the percentage of transactions that are " +
                                                     "captured. Setting this to 0 discards all trace data. " +
                                                     "Setting this to 1.0 captures all."),
                (float)options.TracesSampleRate);
            options.TracesSampleRate = Mathf.Clamp01(sampleRate);

            EditorGUI.BeginDisabledGroup(options.TracesSampleRate <= 0);

            GUILayout.Label("Auto Instrumentation", EditorStyles.boldLabel);

            options.AutoStartupTraces = EditorGUILayout.Toggle(
                new GUIContent("Startup Traces ", "Whether the SDK should automatically create " +
                                                       "traces during startup. This integration is currently " +
                                                       "unavailable on WebGL."),
                options.AutoStartupTraces);

            options.AutoSceneLoadTraces = EditorGUILayout.Toggle(
                new GUIContent("Scene Traces ", "Whether the SDK should automatically create traces " +
                                                     "during scene loading. Requires Unity 2020.3 or newer."),
                options.AutoSceneLoadTraces);

            EditorGUILayout.Space();

            GUILayout.Label("Instrumentation through IL Weaving", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("The SDK will modify the compiled assembly during a post build step " +
                                    "to create transaction and spans automatically.", MessageType.Info);

            options.AutoAwakeTraces = EditorGUILayout.Toggle(
                new GUIContent("Awake Calls", "Whether the SDK automatically captures all instances " +
                                              "of Awake as Spans."),
                options.AutoAwakeTraces);

            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
        }
    }
}
