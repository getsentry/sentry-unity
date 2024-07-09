using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

internal static class AdvancedTab
{
    private static bool UnfoldFailedStatusCodeRanges;

    internal static void Display(ScriptableSentryUnityOptions options, SentryCliOptions? cliOptions)
    {
        {
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
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.AnrDetectionEnabled = EditorGUILayout.BeginToggleGroup(
                new GUIContent("ANR Detection", "Whether the SDK should report 'Application Not " +
                                                "Responding' events."),
                options.AnrDetectionEnabled);

            options.AnrTimeout = EditorGUILayout.IntField(
                new GUIContent("ANR Timeout [ms]", "The duration in [ms] for how long the game has to be unresponsive " +
                                                   "before an ANR event is reported.\nDefault: 5000ms"),
                options.AnrTimeout);
            options.AnrTimeout = Math.Max(0, options.AnrTimeout);

            EditorGUILayout.EndToggleGroup();
        }

        EditorGUILayout.Space();
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        EditorGUILayout.Space();

        {
            options.CaptureFailedRequests = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Capture Failed HTTP Requests",
                    "Whether the SDK should capture failed HTTP requests. This works out of the box for iOS only" +
                    "For the C# layer you need to add the 'SentryHttpMessageHandler' to your HTTP Client."),
                options.CaptureFailedRequests);

            UnfoldFailedStatusCodeRanges = EditorGUILayout.BeginFoldoutHeaderGroup(UnfoldFailedStatusCodeRanges, "Failed Status Codes Ranges");
            if (UnfoldFailedStatusCodeRanges)
            {
                var rangeCount = options.FailedRequestStatusCodes.Count / 2;
                rangeCount = EditorGUILayout.IntField(
                    new GUIContent("Status Codes Range Count", "The amount of ranges of HTTP status codes to capture."),
                    rangeCount);

                // Because it's a range, we need to double the count
                rangeCount *= 2;

                if (rangeCount <= 0)
                {
                    options.FailedRequestStatusCodes.Clear();
                }

                if (rangeCount < options.FailedRequestStatusCodes.Count)
                {
                    options.FailedRequestStatusCodes.RemoveRange(rangeCount, options.FailedRequestStatusCodes.Count - rangeCount);
                }

                if (rangeCount > options.FailedRequestStatusCodes.Count)
                {
                    var rangedToAdd = rangeCount - options.FailedRequestStatusCodes.Count;
                    for (var i = 0; i < rangedToAdd; i += 2)
                    {
                        options.FailedRequestStatusCodes.Add(500);
                        options.FailedRequestStatusCodes.Add(599);
                    }
                }

                for (var i = 0; i < options.FailedRequestStatusCodes.Count; i += 2)
                {
                    GUILayout.BeginHorizontal();

                    options.FailedRequestStatusCodes[i] = EditorGUILayout.IntField("Start", options.FailedRequestStatusCodes[i]);
                    options.FailedRequestStatusCodes[i + 1] = EditorGUILayout.IntField("End", options.FailedRequestStatusCodes[i + 1]);

                    GUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
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

        GUILayout.Label("Native Support", EditorStyles.boldLabel);

        {
            options.IosNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("iOS Native Support", "Whether to enable Native iOS support to capture" +
                                                     "errors written in languages such as Objective-C, Swift, C and C++."),
                options.IosNativeSupportEnabled);

            options.AndroidNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Android Native Support", "Whether to enable Native Android support to " +
                                                         "capture errors written in languages such as Java, Kotlin, C and C++."),
                options.AndroidNativeSupportEnabled);

            if (options.AndroidNativeSupportEnabled
                && PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
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
            options.PostGenerateGradleProjectCallbackOrder = EditorGUILayout.IntField(
                new GUIContent("Android Callback Order", "Override the default callback order of " +
                                                         "Sentry Gradle modification script that adds Sentry dependencies " +
                                                         "to the gradle project files."),
                options.PostGenerateGradleProjectCallbackOrder);
            EditorGUI.indentLevel--;

            options.WindowsNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Windows Native Support", "Whether to enable native crashes support on Windows."),
                options.WindowsNativeSupportEnabled);

            options.MacosNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("macOS Native Support", "Whether to enable native crashes support on macOS."),
                options.MacosNativeSupportEnabled);

            options.LinuxNativeSupportEnabled = EditorGUILayout.Toggle(
                new GUIContent("Linux Native Support", "Whether to enable native crashes support on Linux."),
                options.LinuxNativeSupportEnabled);
        }

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
                    EditorGUILayout.HelpBox("The IL2CPP line number support relies on the Debug Symbol Upload to be properly set up.", MessageType.Error);
                }
            }
        }
    }
}
