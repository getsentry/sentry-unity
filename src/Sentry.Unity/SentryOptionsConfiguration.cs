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

        #if UNITY_ANDROID || UNITY_IOS
                // Important!
                // On Android and iOS, ALL options configured here will be "baked" into the exported project during
                // build time. Any runtime changes to these options will not take effect.

                // Examples:

                // Works as expected and will disable all debug logging of the native SDKs
                // options.Debug = false;

                // Will NOT work as expected as this will need to get validated at runtime
                // options.Debug = SystemInfo.deviceName.Contains("Pixel");
        #endif
            }
        }
        """;

    /// <summary>
    /// Called during build and during the game's startup to configure the options used to initialize the SDK
    /// </summary>
    public abstract void Configure(SentryUnityOptions options);
}
