using System;
using Sentry.Unity;
using UnityEngine;

public class SentryCliConfiguration : SentryCliOptionsConfiguration
{
    // The auth token comes from the environment the Editor or the build was started in, so it never lands in the
    // project. A build copies it into its own sentry.properties, which git ignores.
    public override void Configure(SentryCliOptions cliOptions)
    {
        var token = Environment.GetEnvironmentVariable("SENTRY_AUTH_TOKEN");
        var hasToken = !string.IsNullOrEmpty(token);

        cliOptions.Auth = token;
        cliOptions.UploadSymbols = hasToken;
        cliOptions.UploadSources = hasToken;

        if (!hasToken)
        {
            Debug.Log("Debug symbol upload is off: SENTRY_AUTH_TOKEN is not set in the environment the Editor or the build was started from.");
        }
    }
}
