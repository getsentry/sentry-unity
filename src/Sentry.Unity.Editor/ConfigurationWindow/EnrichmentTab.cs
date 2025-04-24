using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class EnrichmentTab
{
    internal static void Display(ScriptableSentryUnityOptions options)
    {
        {
            GUILayout.Label("If the SDK should include data that potentially includes PII, such as Machine Name", EditorStyles.boldLabel);

            options.SendDefaultPii = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Send default PII", "Whether to include default Personal Identifiable " +
                                                   "Information."),
                options.SendDefaultPii);
            EditorGUI.indentLevel++;

            options.IsEnvironmentUser = EditorGUILayout.Toggle(
                new GUIContent("Auto Set UserName", "Whether to report the 'Environment.UserName' as " +
                                                    "the User affected in the event. Should be disabled for " +
                                                    "Android and iOS."),
                options.IsEnvironmentUser);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.AttachScreenshot = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Attach Screenshots to Events", "Try to attach current screenshot on events.\n" +
                                                    "This is an early-access feature and may not work on all platforms (it is explicitly disabled on WebGL).\n" +
                                                    "Additionally, the screenshot is captured mid-frame, when an event happens, so it may be incomplete.\n" +
                                                    "A screenshot might not be able to be attached, for example when the error happens on a background thread."),
                options.AttachScreenshot);
            EditorGUI.indentLevel++;

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

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.AttachViewHierarchy = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Attach Hierarchy to Events", "Try to attach the current scene's hierarchy."),
                options.AttachViewHierarchy);
            EditorGUI.indentLevel++;

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

            EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
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
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.ReportAssembliesMode = (ReportAssembliesMode)EditorGUILayout.EnumPopup(
                new GUIContent("Report Assemblies Mode", "Whether or not to include referenced assemblies " +
                                                         "Version or InformationalVersion in each event sent to sentry."),
                options.ReportAssembliesMode);
        }
    }
}
