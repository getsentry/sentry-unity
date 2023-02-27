using UnityEngine;
using Sentry.Unity;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/BuildTimeOptionsConfiguration.asset", menuName = "Sentry/BuildTimeOptionsConfiguration", order = 999)]
public class BuildTimeOptionsConfiguration : Sentry.Unity.SentryBuildTimeOptionsConfiguration
{
    /// Called during app build. Changes made here will affect build-time processing, symbol upload, etc.
    /// Additionally, because iOS, macOS and Android native error handling is configured at build time,
    /// you can make changes to these options here.
    /// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
    public override void Configure(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log(nameof(BuildTimeOptionsConfiguration) + "::Configure() called");

        // Changes to the options object that will be used during build.
        // This means you have access to the Sentry CLI options and also the options that affect the native layer
        // on Android, iOS and macOS
        // As an example: We use this as part of our integration tests here: https://github.com/getsentry/sentry-unity/blob/main/test/Scripts.Integration.Test/Editor/BuildTimeOptions.cs

        Debug.Log(nameof(BuildTimeOptionsConfiguration) + "::Configure() finished");
    }
}
