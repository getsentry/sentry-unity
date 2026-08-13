using System;
using Sentry.Unity;
using UnityEngine;

public class CliConfiguration : SentryCliOptionsConfiguration
{
    public override void Configure(SentryCliOptions cliOptions)
    {
        Debug.Log("Sentry: CliConfiguration::Configure() called");

        var authToken = Environment.GetEnvironmentVariable("SENTRY_AUTH_TOKEN");
        cliOptions.UploadSymbols = !string.IsNullOrEmpty(authToken);
        cliOptions.UploadSources = cliOptions.UploadSymbols;
        cliOptions.Auth = authToken;

        cliOptions.Organization = "sentry-sdks";
        cliOptions.Project = "sentry-unity-integration-tests";

        // sentry-cli derives its upload URL from the DSN whenever that DSN is not sentry.io. During
        // envelope capture the DSN points at the local capture server, which would send symbol
        // uploads there too. Pin the CLI to sentry.io so symbol upload keeps working either way.
        cliOptions.UrlOverride = "https://sentry.io";

        Debug.Log("Sentry: CliConfiguration::Configure() finished");
    }
}
