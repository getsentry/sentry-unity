using System;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif
using Sentry.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        // Here you can programmatically modify the Sentry option properties used for the SDK's initialization

        Debug.Log("SentryOptionConfigure started.");

        // This is making sure the SDK is not already initialized during tests for local development
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != null && sceneName.Contains("TestScene"))
        {
            Debug.Log("Disabling the SDK while running tests.");
            options.Enabled = false;
        }

        // BeforeSend is currently limited to C# code. Native errors - such as crashes in C/C++ code - are getting
        // captured by the native SDKs, but the native SDKs won't invoke this callback.
        options.SetBeforeSend((sentryEvent, _) =>
        {
            if (sentryEvent.Tags.ContainsKey("SomeTag"))
            {
                // Don't send events with a tag SomeTag to Sentry
                return null;
            }

            return sentryEvent;
        });

        options.SetBeforeSendLog(log =>
        {
            // You can filter logs based on tags
            if (log.Message.StartsWith("Sensitive:"))
            {
                return null;
            }

            return log;
        });

        // Native SDK initialization timing options:
        // Build-time initialization:
        //   + Can capture Unity engine errors
        //   - Options are fixed at build time
        // Runtime initialization:
        //   + Allows dynamic configuration
        //   - Miss some early errors that happen before the SDK initialized
#if UNITY_ANDROID
        options.AndroidNativeInitializationType = NativeInitializationType.Runtime;
        ConfigureSessionReplay(options);
#elif UNITY_IOS
        options.IosNativeInitializationType = NativeInitializationType.Runtime;
        ConfigureSessionReplay(options);
#endif

        Debug.Log("SentryOptionConfigure finished.");
    }

#if UNITY_ANDROID || UNITY_IOS
    // Experimental session replay. Enabled at build time, so the Android build carries the replay library. At runtime,
    // launch arguments pick the mode and what captures the frames. On Android, launch extras:
    // `adb shell am start -n <package>/com.unity3d.player.UnityPlayerActivity --es sentry_replay off|session|error
    // --es sentry_replay_capture native|unity`. On iOS, process arguments:
    // `xcrun devicectl device process launch --device <device> <bundle id> -sentry_replay off|session|error
    // -sentry_replay_capture native|unity`. Without them, replays are sent for errors, with frames the native SDK
    // captures.
    private static void ConfigureSessionReplay(SentryUnityOptions options)
    {
        var mode = Application.isEditor ? null : GetLaunchArgument("sentry_replay");
        var capture = Application.isEditor ? null : GetLaunchArgument("sentry_replay_capture");
        var replay = options.Experimental.SessionReplay;
        switch (mode)
        {
            case "off":
                break;
            case "session":
                replay.SessionSampleRate = 1.0;
                break;
            default:
                replay.OnErrorSampleRate = 1.0;
                break;
        }

        replay.Capture = capture == "unity" ? SessionReplayCapture.Unity : SessionReplayCapture.Native;

        Debug.Log($"Session replay mode: {mode ?? "error"}, capture: {replay.Capture}");
    }
#endif

#if UNITY_ANDROID
    internal static string GetLaunchArgument(string name)
    {
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = activity.Call<AndroidJavaObject>("getIntent");
            return intent.Call<string>("getStringExtra", name);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to read the launch extra '{name}': {e.Message}");
            return null;
        }
    }
#elif UNITY_IOS
    internal static string GetLaunchArgument(string name) => sampleLaunchArgument(name);

    // ObjectiveCPlugin.m
    [DllImport("__Internal")]
    private static extern string sampleLaunchArgument(string name);
#endif
}
