using System;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_WEBGL
using System.Web;
#endif

public class TestLauncher : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("TestLauncher, awake!");
        Application.quitting += () =>
        {
            // Keep "SmokeTester is quitting." for backward compatibility with smoke-test-android.ps1
            // and run-smoke-test.ps1 which look for this exact string to detect test completion.
            Debug.Log("SmokeTester is quitting.");
        };
    }

    public void Start()
    {
        var arg = GetTestArg();
        Debug.Log($"TestLauncher arg: '{arg}'");

        switch (arg)
        {
            // Legacy smoke test commands -> SmokeTester
            case "smoke":
            case "crash":
            case "has-crashed":
            case "hasnt-crashed":
                gameObject.AddComponent<SmokeTester>();
                break;

            // Integration test commands -> IntegrationTester
            case "message-capture":
            case "exception-capture":
            case "crash-capture":
            case "crash-send":
                gameObject.AddComponent<IntegrationTester>();
                break;

            default:
                Debug.LogError($"Unknown test command: {arg}");
                Application.Quit(1);
                break;
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal", EntryPoint="getTestArgObjectiveC")]
    private static extern string GetTestArg();
#else
    internal static string GetTestArg()
    {
        string arg = null;
#if UNITY_EDITOR
#elif UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
        {
            arg = intent.Call<String>("getStringExtra", "test");
        }
#elif UNITY_WEBGL
        var uri = new Uri(Application.absoluteURL);
        arg = HttpUtility.ParseQueryString(uri.Query).Get("test");
#else
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 2 && args[1] == "--test")
        {
            arg = args[2];
        }
#endif
        return arg;
    }
#endif
}
