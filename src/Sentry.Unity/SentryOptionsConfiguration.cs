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
            }
        }
        """;

    /// <summary>
    /// Called at the player startup by SentryInitialization.
    /// You can alter configuration for the C# error handling.
    /// </summary>
    public abstract void Configure(SentryUnityOptions options);
}
