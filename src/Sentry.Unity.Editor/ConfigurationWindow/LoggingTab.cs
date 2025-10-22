using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class LoggingTab
{
    internal static void Display(ScriptableSentryUnityOptions options)
    {
        {
            GUILayout.Label("Structured Logging - Experimental", EditorStyles.boldLabel);

            options.EnableStructuredLogging = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Send Logs for:", "Enables the SDK to forward log messages to Sentry " +
                                                 "based on the log level."),
                options.EnableStructuredLogging);

            EditorGUI.indentLevel++;

            options.OnDebugLog = EditorGUILayout.Toggle(
                new GUIContent("Debug.Log",
                    "Whether the SDK should forward Debug.Log messages to Sentry structured logging"),
                options.OnDebugLog);
            options.OnDebugLogWarning = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogWarning",
                    "Whether the SDK should forward Debug.LogWarning messages to Sentry structured logging"),
                options.OnDebugLogWarning);
            options.OnDebugLogAssertion = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogAssertion",
                    "Whether the SDK should forward Debug.LogAssertion messages to Sentry structured logging"),
                options.OnDebugLogAssertion);
            options.OnDebugLogError = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogError",
                    "Whether the SDK should forward Debug.LogError messages to Sentry structured logging"),
                options.OnDebugLogError);
            options.OnDebugLogException = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogException",
                    "Whether the SDK should forward Debug.LogException messages to Sentry structured logging"),
                options.OnDebugLogException);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            GUILayout.Label("Breadcrumbs", EditorStyles.boldLabel);

            if (options.EnableStructuredLogging)
            {
                options.AttachBreadcrumbsToEvents = EditorGUILayout.BeginToggleGroup(
                    new GUIContent("Attach logs as breadcrumbs in addition to sending them as structured logs", "Whether the SDK should attach breadcrumbs to events in addition to structured logging."),
                    options.AttachBreadcrumbsToEvents);

                GUILayout.Label("Note: With sending structured logs enabled you have to opt-into adding breadcrumbs to events.", EditorStyles.boldLabel);
            }

            EditorGUI.indentLevel++;

            options.BreadcrumbsForLogs = EditorGUILayout.Toggle(
                new GUIContent("Debug.Log", "Whether the SDK automatically adds breadcrumbs 'Debug.Log'."),
                options.BreadcrumbsForLogs);
            options.BreadcrumbsForWarnings = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogWarning", "Whether the SDK automatically adds breadcrumbs for 'Debug.LogWarning'."),
                options.BreadcrumbsForWarnings);
            options.BreadcrumbsForAsserts = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogAssertion", "Whether the SDK automatically adds breadcrumbs for 'Debug.LogAssertion'."),
                options.BreadcrumbsForAsserts);
            options.BreadcrumbsForErrors = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogError", "Whether the SDK automatically adds breadcrumbs for 'Debug.LogError'."),
                options.BreadcrumbsForErrors);

            EditorGUILayout.Space();

            options.MaxBreadcrumbs = EditorGUILayout.IntField(
                new GUIContent("Max Breadcrumbs", "Maximum number of breadcrumbs that get captured." +
                                                  "\nDefault: 100"),
                options.MaxBreadcrumbs);
            options.MaxBreadcrumbs = Math.Max(0, options.MaxBreadcrumbs);

            EditorGUI.indentLevel--;
            if (options.EnableStructuredLogging)
            {
                EditorGUILayout.EndToggleGroup();
            }
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            GUILayout.Label("CaptureMessage Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            options.CaptureLogErrorEvents = EditorGUILayout.Toggle(
                new GUIContent("Capture LogError", "Whether the SDK automatically captures events for 'Debug.LogError'."),
                options.CaptureLogErrorEvents);

            options.AttachStacktrace = EditorGUILayout.Toggle(
                new GUIContent("Attach Stack Trace", "Whether the SDK should include a stack trace for CaptureMessage " +
                                                     "events. Refer to AttachStacktrace on sentry docs."),
                options.AttachStacktrace);

            GUILayout.Label("Note: The stack trace quality will depend on the IL2CPP line number setting and might not contain line numbers.", EditorStyles.boldLabel);

            // Enhanced not supported on IL2CPP so not displaying this for the time being:
            // Options.StackTraceMode = (StackTraceMode) EditorGUILayout.EnumPopup(
            //     new GUIContent("Stacktrace Mode", "Enhanced is the default." +
            //                                       "\n - Enhanced: Include async, return type, args,..." +
            //                                       "\n - Original - Default .NET stack trace format."),
            //     Options.StackTraceMode);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.EnableLogDebouncing = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Enable Log Debouncing", "The SDK debounces log messages of the " +
                                                        "same type if they are more frequent than once per second."),
                options.EnableLogDebouncing);

            EditorGUI.indentLevel++;

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

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
        }
    }
}
