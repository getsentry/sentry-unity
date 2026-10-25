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

        // sentry-cli's default 5 retries give up within ~30s of sentry.io answering 503/504. Reaches the uploads the Editor spawns; iOS sets it on xcodebuild.
        if (Environment.GetEnvironmentVariable("SENTRY_HTTP_MAX_RETRIES") is null)
        {
            Environment.SetEnvironmentVariable("SENTRY_HTTP_MAX_RETRIES", "20");
        }

        cliOptions.Organization = "sentry-sdks";
        cliOptions.Project = "sentry-unity-integration-tests";

        Debug.Log("Sentry: CliConfiguration::Configure() finished");
    }
}
