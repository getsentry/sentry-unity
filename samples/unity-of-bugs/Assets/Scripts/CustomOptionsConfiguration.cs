using Sentry.Unity;
using UnityEngine;

/// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
[CreateAssetMenu(fileName = "Assets/Resources/Sentry/CustomOptionsConfiguration.cs", menuName = "Sentry/CustomOptionsConfiguration", order = 999)]
public class CustomOptionsConfiguration : ScriptableOptionsConfiguration
{
    /// See base class for documentation.
    public override void ConfigureAtBuild(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("CustomOptionsConfiguration::ConfigureAtBuild called");
    }

    /// See base class for documentation.
    public override void ConfigureAtRuntime(SentryUnityOptions options)
    {
        Debug.Log("CustomOptionsConfiguration::ConfigureAtRuntime called");
        options.BeforeSend = sentryEvent =>
        {
            if (sentryEvent.Tags.ContainsKey("SomeTag"))
            {
                // Don't send events with a tag SomeTag to Sentry
                return null;
            }

            return sentryEvent;
        };
    }
}
