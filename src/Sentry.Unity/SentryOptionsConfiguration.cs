using UnityEngine;

namespace Sentry.Unity;

public abstract class SentryOptionsConfiguration : ScriptableObject
{
    public static readonly string Template =
        """
        using Sentry;
        using Sentry.Unity;

        public class {{ScriptName}} : SentryOptionsConfiguration
        {
            public override void Configure(SentryUnityOptions options)
            {
                // Here you can programmatically modify the Sentry option properties used for the SDK's initialization

        #if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX
                // NOTE!
                // On Android, iOS, and macOS, ALL options configured here will be "baked" into the exported project
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
                *                                │
                *               ┌───────────────────────────────────┐
                *               │      Options used for Init        │
                *               │                                   │
                *               ▼                                   ▼
                *  ┌──────────────────────────┐         ┌──────────────────────┐
                *  │        Native SDK        │         │     Unity C# SDK     │
                *  │    Android/iOS/macOS)    │         │    Initialization    │
                *  │  ┌────────────────────┐  │         └──────────────────────┘
                *  │  │ Options "Baked in" │  │
                *  │  └────────────────────┘  │
                *  │  The configure call made │
                *  │  for this part ran on    │
                *  │  your build-machine      │
                *  └──────────────────────────┘
                *               │
                *               │
                *               ▼
                *  ┌──────────────────────────┐
                *  │         Native SDK       │
                *  │    Android/iOS/macOS)    │
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
            }
        }
        """;

    /// <summary>
    /// Called during build and during the game's startup to configure the options used to initialize the SDK
    /// </summary>
    public abstract void Configure(SentryUnityOptions options);
}
