using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    internal static class EnrichmentTab
    {
        internal static void Display(ScriptableSentryUnityOptions options)
        {
            GUILayout.Label("Tag Overrides", EditorStyles.boldLabel);

            options.ReleaseOverride = EditorGUILayout.TextField(
                new GUIContent("Override Release", "By default release is built from " +
                                                   "'Application.productName'@'Application.version'. " +
                                                   "This option is an override."),
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

            {
                options.AttachScreenshot = EditorGUILayout.BeginToggleGroup(
                    new GUIContent("Attach Screnshot", "Try to attach current screenshot for events.\n" +
                        "A screenshot might not be able to be attached, for example when the error happens on a background thread."),
                    options.AttachScreenshot);
                options.ScreenshotMaxWidth = EditorGUILayout.IntField(
                    new GUIContent("Max Width", "Maximum width of the screenshot or 0 to keep the original size. " +
                        "If the application window is larger, the screenshot will be resized proportionally."),
                    options.ScreenshotMaxWidth);
                options.ScreenshotMaxHeight = EditorGUILayout.IntField(
                    new GUIContent("Max Height", "Maximum height of the screenshot or 0 to keep the original size. " +
                        "If the application window is larger, the screenshot will be resized proportionally."),
                    options.ScreenshotMaxHeight);
                options.ScreenshotQuality = EditorGUILayout.IntSlider(
                    new GUIContent("JPG quality", "Quality of the JPG screenshot: 0 - 100, where 100 is the best quality and highest size."),
                    options.ScreenshotQuality, 0, 100);
                EditorGUILayout.EndToggleGroup();
            }
        }
    }
}
