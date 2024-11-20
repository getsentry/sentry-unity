using Sentry.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        // Here you can programmatically modify the Sentry option properties used for the SDK's initialization

#if UNITY_ANDROID || UNITY_IOS
        // NOTE!
        // On Android and iOS, ALL options configured here will be "baked" into the exported project
        // during the build process.
        // Changes to the options at runtime will not affect the native SDKs (Java, C/C++, Objective-C)
        // and only apply to the C# layer.

        /*
        * Sentry Unity SDK - Hybrid Architecture
        * ======================================
        *
        *          Build Time                          Runtime
        *  ┌─────────────────────────┐        ┌─────────────────────────┐
        *  │      Unity Editor       │        │       Game Startup      │
        *  └──────────┬──────────────┘        └───────────┬─────────────┘
        *             │                                   │
        *             ▼                                   ▼
        *  ┌────────────────────────────────────────────────────────────┐
        *  │                    Options Configuration                   │
        *  │                       (This Method)                        │
        *  └─────────────────────────────┬──────────────────────────────┘
        *                                │
        *               ┌───────────────────────────────────┐
        *               │      Options used for Init        │
        *               ▼                                   ▼
        *  ┌──────────────────────────┐         ┌──────────────────────┐
        *  │        Native SDK        │         │     Unity C# SDK     │
        *  │       Android & iOS      │         │    Initialization    │
        *  │  ┌────────────────────┐  │         └──────────────────────┘
        *  │  │ Options "Baked in" │  │
        *  │  └────────────────────┘  │
        *  │  The configure call made │
        *  │  for this part ran on    │
        *  │  your build-machine      │
        *  └──────────────────────────┘
        *               │
        *               ▼
        *  ┌──────────────────────────┐
        *  │        Native SDK        │
        *  │       Android & iOS      │
        *  └──────────────────────────┘
        */

        // Works as expected and will enable all debug logging
        // options.Debug = true;

        // Will NOT work as expected.
        // This will run twice.
        //    1. Once during the build, being baked into the native SDKs
        //    2. And a second time every time when the game starts
        // options.Release = ComputeVersion();
#endif

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

        Debug.Log("OptionConfigure finished.");
    }
}
