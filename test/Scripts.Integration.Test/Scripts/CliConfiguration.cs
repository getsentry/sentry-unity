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

        // No UrlOverride on purpose: sentry-cli derives its upload URL from the DSN, so during
        // envelope capture the debug files go to the capture server and end up in the artifact.

        Debug.Log("Sentry: CliConfiguration::Configure() finished");
    }
}
