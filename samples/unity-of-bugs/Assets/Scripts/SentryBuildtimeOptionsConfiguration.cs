using UnityEngine;
using Sentry.Unity;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/SentryBuildtimeOptionsConfiguration.asset", menuName = "Sentry/Assets/Resources/Sentry/SentryBuildtimeOptionsConfiguration.asset", order = 999)]
public class SentryBuildtimeOptionsConfiguration : Sentry.Unity.BuildtimeOptionsConfiguration
{
    /// See base class for documentation.
    /// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
    public override void Configure(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("SentryBuildtimeOptionsConfiguration::Configure() called");
        Debug.Log("SentryBuildtimeOptionsConfiguration::Configure() finished");
    }
}
