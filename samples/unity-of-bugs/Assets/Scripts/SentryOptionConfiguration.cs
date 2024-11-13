using Sentry;
using Sentry.Unity;
using UnityEngine;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        // Here you can programmatically modify the Sentry option properties used for the SDK's initialization

#if UNITY_ANDROID || UNITY_IOS
        // Important!
        // Android and iOS options are getting validated and set at build time. Changes to them while the game
        // On Android and iOS, ALL options configured here will be "baked" into the exported project during
        // build time. Any runtime changes to these options will not take effect.

        // Works as expected and will disable all debug logging of the native SDKs
        // options.Debug = false;

        // Will NOT work as expected as this will need to get validated at runtime
        // options.Debug = SystemInfo.deviceName.Contains("Pixel");
#endif
    }
}
