using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class EnrichmentTab
{
    internal static void Display(ScriptableSentryUnityOptions options)
    {
        GUILayout.Label("Tag Overrides", EditorStyles.boldLabel);

        options.ReleaseOverride = EditorGUILayout.TextField(
            new GUIContent("Override Release", "By default release is built from the Application info as: " +
                                               "\"{productName}@{version}+{buildGUID}\". " +
                                               "\nThis option is an override."),
            options.ReleaseOverride);

        options.EnvironmentOverride = EditorGUILayout.TextField(
            new GUIContent("Override Environment", "Auto detects 'production' or 'editor' by " +
                                                   "default based on 'Application.isEditor." +
                                                   "\nThis option is an override."),
            options.EnvironmentOverride);

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        GUILayout.Label("Stacktrace", EditorStyles.boldLabel);

        options.AttachStacktrace = EditorGUILayout.Toggle(
            new GUIContent("Stacktrace For Logs", "Whether to include a stack trace for non " +
                                                  "error events like logs. Even when Unity didn't include and no " +
                                                  "exception was thrown. Refer to AttachStacktrace on sentry docs."),
            options.AttachStacktrace);

        // Enhanced not supported on IL2CPP so not displaying this for the time being:
        // Options.StackTraceMode = (StackTraceMode) EditorGUILayout.EnumPopup(
        //     new GUIContent("Stacktrace Mode", "Enhanced is the default." +
        //                                       "\n - Enhanced: Include async, return type, args,..." +
        //                                       "\n - Original - Default .NET stack trace format."),
        //     Options.StackTraceMode);

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.SendDefaultPii = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Send default PII", "Whether to include default Personal Identifiable " +
                                                   "Information."),
                options.SendDefaultPii);

            options.IsEnvironmentUser = EditorGUILayout.Toggle(
                new GUIContent("Auto Set UserName", "Whether to report the 'Environment.UserName' as " +
                                                    "the User affected in the event. Should be disabled for " +
                                                    "Android and iOS."),
                options.IsEnvironmentUser);
            EditorGUILayout.EndToggleGroup();
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        options.CaptureLogErrorEvents = EditorGUILayout.Toggle(
            new GUIContent("Capture LogError as Event", "Whether the SDK automatically captures events for 'Debug.LogError'."),
            options.CaptureLogErrorEvents);

        GUILayout.Label("Breadcrumbs automatically added for LogType:", EditorStyles.boldLabel);

        options.BreadcrumbsForLogs = EditorGUILayout.Toggle(
            new GUIContent("Log", "Whether the SDK automatically adds breadcrumbs 'Debug.Log'."),
            options.BreadcrumbsForLogs);
        options.BreadcrumbsForWarnings = EditorGUILayout.Toggle(
            new GUIContent("Warning", "Whether the SDK automatically adds breadcrumbs for 'Debug.LogWarning'."),
            options.BreadcrumbsForWarnings);
        options.BreadcrumbsForAsserts = EditorGUILayout.Toggle(
            new GUIContent("Assert", "Whether the SDK automatically adds breadcrumbs for 'Debug.Assert'."),
            options.BreadcrumbsForAsserts);
        options.BreadcrumbsForErrors = EditorGUILayout.Toggle(
            new GUIContent("Error", "Whether the SDK automatically adds breadcrumbs for 'Debug.LogError'."),
            options.BreadcrumbsForErrors);
        options.BreadcrumbsForExceptions = EditorGUILayout.Toggle(
            new GUIContent("Exception", "Whether the SDK automatically adds breadcrumbs for exceptions and 'Debug.LogException'."),
            options.BreadcrumbsForExceptions);

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        options.MaxBreadcrumbs = EditorGUILayout.IntField(
            new GUIContent("Max Breadcrumbs", "Maximum number of breadcrumbs that get captured." +
                                              "\nDefault: 100"),
            options.MaxBreadcrumbs);
        options.MaxBreadcrumbs = Math.Max(0, options.MaxBreadcrumbs);

        options.ReportAssembliesMode = (ReportAssembliesMode)EditorGUILayout.EnumPopup(
            new GUIContent("Report Assemblies Mode", "Whether or not to include referenced assemblies " +
                                                     "Version or InformationalVersion in each event sent to sentry."),
            options.ReportAssembliesMode);

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        options.AttachScreenshot = EditorGUILayout.BeginToggleGroup(
            new GUIContent("Attach Screenshot", "Try to attach current screenshot on events.\n" +
                                                "This is an early-access feature and may not work on all platforms (it is explicitly disabled on WebGL).\n" +
                                                "Additionally, the screenshot is captured mid-frame, when an event happens, so it may be incomplete.\n" +
                                                "A screenshot might not be able to be attached, for example when the error happens on a background thread."),
            options.AttachScreenshot);

        options.ScreenshotQuality = (ScreenshotQuality)EditorGUILayout.EnumPopup(
            new GUIContent("Quality", "The resolution quality of the screenshot.\n" +
                                      "'Full': Fully of the current resolution\n" +
                                      "'High': 1080p\n" +
                                      "'Medium': 720p\n" +
                                      "'Low': 480p"),
            options.ScreenshotQuality);

        options.ScreenshotCompression = EditorGUILayout.IntSlider(
            new GUIContent("Compression", "The compression of the screenshot."),
            options.ScreenshotCompression, 1, 100);

        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();
        {
            options.AttachViewHierarchy = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Attach Hierarchy", "Try to attach the current scene's hierarchy."),
                options.AttachViewHierarchy);

            options.MaxViewHierarchyRootObjects = EditorGUILayout.IntField(
                new GUIContent("Max Root GameObjects", "Maximum number of captured GameObjects in " +
                                                       "a scene root." +
                                                       "\nDefault: 100"),
                options.MaxViewHierarchyRootObjects);
            if (options.MaxViewHierarchyRootObjects <= 0)
            {
                options.MaxViewHierarchyRootObjects = 0;
            }

            options.MaxViewHierarchyObjectChildCount = EditorGUILayout.IntField(
                new GUIContent("Max Child Count Per Object", "Maximum number of child objects " +
                                                             "captured for each GameObject." +
                                                             "\nDefault: 20"),
                options.MaxViewHierarchyObjectChildCount);
            if (options.MaxViewHierarchyObjectChildCount <= 0)
            {
                options.MaxViewHierarchyObjectChildCount = 0;
            }

            options.MaxViewHierarchyDepth = EditorGUILayout.IntField(
                new GUIContent("Max Depth", "Maximum depth of the hierarchy to capture. " +
                                            "For example, setting 1 will only capture root GameObjects." +
                                            "\nDefault: 10"),
                options.MaxViewHierarchyDepth);
            if (options.MaxViewHierarchyDepth <= 0)
            {
                options.MaxViewHierarchyDepth = 0;
            }

            EditorGUILayout.EndToggleGroup();
        }
    }
}
