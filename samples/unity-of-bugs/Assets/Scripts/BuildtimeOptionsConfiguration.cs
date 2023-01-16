using UnityEngine;
using Sentry.Unity;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/BuildtimeOptionsConfiguration.asset", menuName = "Sentry/BuildtimeOptionsConfiguration.asset", order = 999)]
public class BuildtimeOptionsConfiguration : SentryBuildtimeOptionsConfiguration
{
    /// See base class for documentation.
    /// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
    public override void Configure(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("SentryBuildtimeOptionsConfiguration::Configure() called");
        Debug.Log("SentryBuildtimeOptionsConfiguration::Configure() finished");
    }
}
