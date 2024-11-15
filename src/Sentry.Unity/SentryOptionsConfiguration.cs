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

                // Examples:

                // Works as expected and will enable all debug logging
                // options.Debug = true;

                // Will NOT work as expected as this will get validated at runtime.
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
