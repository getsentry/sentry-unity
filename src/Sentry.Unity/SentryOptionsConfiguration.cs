using UnityEngine;

namespace Sentry.Unity;

public abstract class SentryOptionsConfiguration : ScriptableObject
{
    public static string GetAssetPath(string scriptName) => $"Assets/Resources/Sentry/{scriptName}.asset";
    public static readonly string Template =
        """
        using Sentry.Unity;

        public class {{ScriptName}} : SentryOptionsConfiguration
        {
            public override void Configure(SentryUnityOptions options)
            {
                // Here you can programmatically modify the Sentry option properties used for the SDK's initialization
            }
        }
        """;

    /// <summary>
    /// Called during build and during the game's startup to configure the options used to initialize the SDK
    /// </summary>
    public abstract void Configure(SentryUnityOptions options);
}
