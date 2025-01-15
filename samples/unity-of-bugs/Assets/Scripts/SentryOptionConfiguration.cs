using Sentry.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        // Here you can programmatically modify the Sentry option properties used for the SDK's initialization

        Debug.Log("OptionConfigure started.");

        // Making sure the SDK is not already initialized during tests
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != null && sceneName.Contains("TestScene"))
        {
            Debug.Log("Disabling the SDK while running tests.");
            options.Enabled = false;
        }

        // BeforeSend is only relevant at runtime. It wouldn't hurt to be set at build time, just wouldn't do anything.
        options.SetBeforeSend((sentryEvent, _) =>
        {
            if (sentryEvent.Tags.ContainsKey("SomeTag"))
            {
                // Don't send events with a tag SomeTag to Sentry
                return null;
            }

            return sentryEvent;
        });

        options.AndroidNativeInitializationType = NativeInitializationType.Runtime;

        Debug.Log("OptionConfigure finished.");
    }
}
