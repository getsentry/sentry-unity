using Sentry;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        // Here you can programmatically modify the Sentry option properties used for the SDK's initialization

#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
        // Important!
        // Android and iOS options are getting validated and set at build time. Changes to them while the game
        // On Android and iOS, ALL options configured here will be "baked" into the exported project during
        // build time. Any runtime changes to these options will not take effect.

        // Works as expected and will disable all debug logging of the native SDKs
        // options.Debug = false;

        // Will NOT work as expected as this will need to get validated at runtime
        // options.Debug = SystemInfo.deviceName.Contains("Pixel");

        Debug.Log(nameof(RuntimeConfiguration) + "::Configure() called");

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

        Debug.Log(nameof(RuntimeConfiguration) + "::Configure() finished");
#endif
    }
}
