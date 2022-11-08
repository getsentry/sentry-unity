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
        Configure(options);
    }

    /// See base class for documentation.
    public override void ConfigureAtRuntime(SentryUnityOptions options)
    {
        Debug.Log("CustomOptionsConfiguration::ConfigureAtRuntime called");
        Configure(options);

        /// BeforeSend is only relevant at runtime. It wouldn't hurt to be set at build time, just wouldn't do anything.
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

    /// Usually, it's a good idea to have common configuration and only call really specific setup in the above methods.
    private void Configure(SentryUnityOptions options)
    {
        Debug.Log("CustomOptionsConfiguration::Configure called");
    }
}
