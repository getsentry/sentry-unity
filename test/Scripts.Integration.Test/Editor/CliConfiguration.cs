using System;
using Sentry.Unity;
using UnityEngine;

public class SentryCliConfiguration : SentryCliOptionsConfiguration
{
    public override void Configure(SentryCliOptions cliOptions)
    {
        Debug.Log("Sentry: SentryCliConfiguration::Configure() called");

        // Assert the deprecated options have been set and are getting overwritten
        Assert.AreEqual("Old configure got called", cliOptions.Auth);

        cliOptions.UploadSymbols = !string.IsNullOrEmpty(cliOptions.UrlOverride);
        cliOptions.UploadSources = cliOptions.UploadSymbols;
        cliOptions.Organization = "sentry-sdks";
        cliOptions.Project = "sentry-unity";
        cliOptions.Auth = "dummy-token";

        Debug.Log("Sentry: SentryCliConfiguration::Configure() finished");
    }
}
