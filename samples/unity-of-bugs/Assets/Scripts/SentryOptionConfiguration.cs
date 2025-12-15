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
#elif UNITY_IOS
        options.IosNativeInitializationType = NativeInitializationType.Runtime;
#endif

        Debug.Log("SentryOptionConfigure finished.");
    }
}
