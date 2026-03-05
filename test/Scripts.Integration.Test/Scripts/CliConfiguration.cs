using System;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.Assertions;

public class CliConfiguration : SentryCliOptionsConfiguration
{
    public override void Configure(SentryCliOptions cliOptions)
    {
        Debug.Log("Sentry: CliConfiguration::Configure() called");

        var authToken = Environment.GetEnvironmentVariable("SENTRY_AUTH_TOKEN");
        if (!string.IsNullOrEmpty(authToken))
        {
            // Upload to real Sentry using the auth token from the environment.
            cliOptions.UploadSymbols = true;
            cliOptions.UploadSources = true;
            cliOptions.Auth = authToken;
        }
        else
        {
            // Upload to a local symbol server for verification (smoke tests).
            cliOptions.UploadSymbols = !string.IsNullOrEmpty(cliOptions.UrlOverride);
            cliOptions.UploadSources = cliOptions.UploadSymbols;
            cliOptions.Auth = "dummy-token";
        }

        cliOptions.Organization = "sentry-sdks";
        cliOptions.Project = "sentry-unity";

        Debug.Log("Sentry: CliConfiguration::Configure() finished");
    }
}
