using UnityEngine;
using Sentry.Unity;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/RuntimeOptionsConfiguration.asset", menuName = "Sentry/RuntimeOptionsConfiguration", order = 999)]
public class RuntimeOptionsConfiguration : Sentry.Unity.SentryRuntimeOptionsConfiguration
{
    /// Called at the player startup by SentryInitialization.
    /// You can alter configuration for the C# error handling and also
    /// native error handling in platforms **other** than iOS, macOS and Android.
    /// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
    public override void Configure(SentryUnityOptions options)
    {
        // Note that changes to the options here will **not** affect iOS, macOS and Android events. (i.e. environment and release)
        // Take a look at `SentryBuildTimeOptionsConfiguration` instead.

        Debug.Log("SentryRuntimeOptionsConfiguration::Configure() called");

        // BeforeSend is only relevant at runtime. It wouldn't hurt to be set at build time, just wouldn't do anything.
        options.BeforeSend = sentryEvent =>
        {
            if (sentryEvent.Tags.ContainsKey("SomeTag"))
            {
                // Don't send events with a tag SomeTag to Sentry
                return null;
            }

            return sentryEvent;
        };

        Debug.Log("SentryRuntimeOptionsConfiguration::Configure() finished");
    }
}
