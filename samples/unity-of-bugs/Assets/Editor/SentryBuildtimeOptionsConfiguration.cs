using System;
using UnityEngine;
using Sentry.Unity;
using Sentry.Unity.Editor;

[CreateAssetMenu(fileName = "Assets/Plugins/Sentry/SentryBuildtimeOptionsConfiguration.asset", menuName = "Sentry/Assets/Plugins/Sentry/SentryBuildtimeOptionsConfiguration.asset", order = 999)]
public class SentryBuildtimeOptionsConfiguration : Sentry.Unity.Editor.ScriptableOptionsConfiguration
{
    /// See base class for documentation.
    /// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
    public override void Configure(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("SentryBuildtimeOptionsConfiguration::Configure() called");
        Debug.Log("SentryBuildtimeOptionsConfiguration::Configure() finished");
    }
}
