using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class LoggingTab
{
    internal static void Display(ScriptableSentryUnityOptions options)
    {
        {
            options.MaxBreadcrumbs = EditorGUILayout.IntField(
                new GUIContent("Max Breadcrumbs", "Maximum number of breadcrumbs that get captured." +
                                                  "\nDefault: 100"),
                options.MaxBreadcrumbs);
            options.MaxBreadcrumbs = Math.Max(0, options.MaxBreadcrumbs);
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
            GUILayout.Label("Automatically capture and send events for:", EditorStyles.boldLabel);

            options.CaptureLogErrorEvents = EditorGUILayout.Toggle(
                new GUIContent("Debug.LogError", "Whether the SDK automatically captures events for 'Debug.LogError'."),
                options.CaptureLogErrorEvents);

            EditorGUILayout.Space();
            GUILayout.Label("Automatically add breadcrumbs for:", EditorStyles.boldLabel);

            options.BreadcrumbsForLogs = EditorGUILayout.Toggle(
                new GUIContent("Debug.Log", "Whether the SDK automatically adds breadcrumbs 'Debug.Log'."),
                options.BreadcrumbsForLogs);
            options.BreadcrumbsForWarnings = EditorGUILayout.Toggle(
                new GUIContent("Debug.Warning", "Whether the SDK automatically adds breadcrumbs for 'Debug.LogWarning'."),
                options.BreadcrumbsForWarnings);
            options.BreadcrumbsForAsserts = EditorGUILayout.Toggle(
                new GUIContent("Debug.Assert", "Whether the SDK automatically adds breadcrumbs for 'Debug.Assert'."),
                options.BreadcrumbsForAsserts);
            options.BreadcrumbsForErrors = EditorGUILayout.Toggle(
                new GUIContent("Debug.Error", "Whether the SDK automatically adds breadcrumbs for 'Debug.LogError'."),
                options.BreadcrumbsForErrors);
            options.BreadcrumbsForExceptions = EditorGUILayout.Toggle(
                new GUIContent("Debug.Exception", "Whether the SDK automatically adds breadcrumbs for exceptions and 'Debug.LogException'."),
                options.BreadcrumbsForExceptions);
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            GUILayout.Label("Create and attach stack trace when capturing a log message. These will not contain line numbers.", EditorStyles.boldLabel);

            options.AttachStacktrace = EditorGUILayout.Toggle(
                new GUIContent("Attach Stack Trace", "Whether to include a stack trace for non " +
                                                      "error events like logs. Even when Unity didn't include and no " +
                                                      "exception was thrown. Refer to AttachStacktrace on sentry docs."),
                options.AttachStacktrace);

            // Enhanced not supported on IL2CPP so not displaying this for the time being:
            // Options.StackTraceMode = (StackTraceMode) EditorGUILayout.EnumPopup(
            //     new GUIContent("Stacktrace Mode", "Enhanced is the default." +
            //                                       "\n - Enhanced: Include async, return type, args,..." +
            //                                       "\n - Original - Default .NET stack trace format."),
            //     Options.StackTraceMode);
        }
    }
}
