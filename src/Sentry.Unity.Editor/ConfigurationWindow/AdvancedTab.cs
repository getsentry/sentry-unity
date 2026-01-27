using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class AdvancedTab
{
    private static bool UnfoldAutomaticOptions;
    private static bool UnfoldNativeOptions;

    internal static void Display(ScriptableSentryUnityOptions options, SentryCliOptions? cliOptions)
    {
        UnfoldAutomaticOptions = EditorGUILayout.BeginFoldoutHeaderGroup(UnfoldAutomaticOptions, "Automatic Behaviour");
        EditorGUI.indentLevel++;
        if (UnfoldAutomaticOptions)
        {
            {
                options.AutoSessionTracking = EditorGUILayout.BeginToggleGroup(
                    new GUIContent("Auto Session Tracking", "Whether the SDK should start and end sessions " +
                                                            "automatically. If the timeout is reached the old session will" +
                                                            "be ended and a new one started."),
                    options.AutoSessionTracking);

                EditorGUI.indentLevel++;
                options.AutoSessionTrackingInterval = EditorGUILayout.IntField(
                    new GUIContent("Timeout [ms]", "The duration of time a session can stay paused " +
                                                           "(i.e. the application has been put in the background) before " +
                                                           "it is considered ended."),
                    options.AutoSessionTrackingInterval);
                options.AutoSessionTrackingInterval = Mathf.Max(0, options.AutoSessionTrackingInterval);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndToggleGroup();
            }

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            {
                options.AnrDetectionEnabled = EditorGUILayout.BeginToggleGroup(
                    new GUIContent("ANR Detection", "Whether the SDK should report 'Application Not " +
                                                    "Responding' events."),
                    options.AnrDetectionEnabled);
                EditorGUI.indentLevel++;

                options.AnrTimeout = EditorGUILayout.IntField(
                    new GUIContent("Timeout [ms]",
                        "The duration in [ms] for how long the game has to be unresponsive " +
                        "before an ANR event is reported.\nDefault: 5000ms"),
                    options.AnrTimeout);
                options.AnrTimeout = Math.Max(0, options.AnrTimeout);

                EditorGUI.indentLevel--;
                EditorGUILayout.EndToggleGroup();
            }

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            {
                GUILayout.Label("Automatic Exception Filter", EditorStyles.boldLabel);

                options.FilterBadGatewayExceptions = EditorGUILayout.Toggle(
                    new GUIContent("BadGatewayException", "Whether the SDK automatically filters Bad Gateway " +
                                                          "exceptions before they are being sent to Sentry."),
                    options.FilterBadGatewayExceptions);

                options.FilterWebExceptions = EditorGUILayout.Toggle(
                    new GUIContent("WebException", "Whether the SDK automatically filters " +
                                                   "System.Net.WebException before they are being sent to Sentry."),
                    options.FilterWebExceptions);

                options.FilterSocketExceptions = EditorGUILayout.Toggle(
                    new GUIContent("SocketException", "Whether the SDK automatically filters " +
                                                      "System.Net.Sockets.SocketException with error code '10049' from " +
                                                      "being sent to Sentry."),
                    options.FilterSocketExceptions);
            }

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndFoldoutHeaderGroup();

        UnfoldNativeOptions = EditorGUILayout.BeginFoldoutHeaderGroup(UnfoldNativeOptions, "Native Support");
        EditorGUI.indentLevel++;
        if (UnfoldNativeOptions)
        {
            GUILayout.Label("Desktop", EditorStyles.boldLabel);

            options.WindowsNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Windows", "Whether to enable native crashes support on Windows."),
                options.WindowsNativeSupportEnabled);

            options.MacosNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("macOS", "Whether to enable native crashes support on macOS."),
                options.MacosNativeSupportEnabled);

            options.LinuxNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Linux", "Whether to enable native crashes support on Linux."),
                options.LinuxNativeSupportEnabled);

            GUILayout.Label("Mobile", EditorStyles.boldLabel);

            options.IosNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("iOS", "Whether to enable Native iOS support to capture" +
                                                     "errors written in languages such as Objective-C, Swift, C and C++."),
                options.IosNativeSupportEnabled);

            options.AndroidNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Android", "Whether to enable Native Android support to " +
                                                         "capture errors written in languages such as Java, Kotlin, C and C++."),
                options.AndroidNativeSupportEnabled);
            if (options.AndroidNativeSupportEnabled
                            && PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                EditorGUILayout.HelpBox("Android native support requires IL2CPP scripting backend and is currently unsupported on Mono.", MessageType.Warning);
            }

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!options.AndroidNativeSupportEnabled);
            options.NdkIntegrationEnabled = EditorGUILayout.Toggle(
                new GUIContent("NDK Integration", "Whether to enable NDK Integration to capture" +
                                                  "errors written in languages such C and C++."),
                options.NdkIntegrationEnabled);
            EditorGUI.BeginDisabledGroup(!options.NdkIntegrationEnabled);
            options.NdkScopeSyncEnabled = EditorGUILayout.Toggle(
                new GUIContent("NDK Scope Sync", "Whether the SDK should sync the scope to the NDK layer."),
                options.NdkScopeSyncEnabled);
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            GUILayout.Label("Console", EditorStyles.boldLabel);

            options.XboxNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Xbox", "Whether to enable native crash support on Xbox."),
                options.XboxNativeSupportEnabled);

            options.PlayStationNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("PlayStation", "Whether to enable native crash support on PlayStation."),
                options.PlayStationNativeSupportEnabled);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.Il2CppLineNumberSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("IL2CPP line numbers", "Whether the SDK should try to to provide line " +
                                                      "numbers for exceptions in IL2CPP builds."),
                options.Il2CppLineNumberSupportEnabled);

            if (options.Il2CppLineNumberSupportEnabled)
            {
                if (!SentryUnityVersion.IsNewerOrEqualThan("2020.3"))
                {
                    EditorGUILayout.HelpBox("The IL2CPP line number feature is supported from Unity version 2020.3 or newer and 2021.3  or newer onwards", MessageType.Warning);
                }
                else if (cliOptions is not null && !cliOptions.IsValid(null, EditorUserBuildSettings.development))
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    var errorIcon = EditorGUIUtility.IconContent("console.erroricon").image;
                    var content = new GUIContent("The IL2CPP line number support relies on the Debug Symbol Upload to be enabled and configured." +
                                                 "\nAdditionally, this requires an Auth Token, an Org-Slug and the Project Name." +
                                                 "\nLearn more about how our IL2CPP support works in our docs.", errorIcon);
                    EditorGUILayout.LabelField(content, EditorStyles.wordWrappedLabel);

                    if (GUILayout.Button("Open Documentation", EditorStyles.linkLabel))
                    {
                        Application.OpenURL("https://docs.sentry.io/platforms/unity/configuration/il2cpp/");
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }
    }
}
