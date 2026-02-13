using System.Collections.Generic;
using Sentry;
using Sentry.Unity;
using UnityEngine;

public class IntegrationOptionsConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        Debug.Log("Sentry: IntegrationOptionsConfig::Configure() called");

        // DSN is read from SentryOptions.asset (baked at build time via configure-sentry.ps1)
        // No custom DSN override needed -- the real DSN is set in the editor configuration.

        options.Environment = "integration-test";
        options.Release = "sentry-unity-test@1.0.0";
        options.Distribution = "test-dist";

        options.AttachScreenshot = true;
        options.Debug = true;
        options.DiagnosticLevel = SentryLevel.Debug;
        options.TracesSampleRate = 1.0d;

        // No custom HTTP handler -- events go to real sentry.io

        // Filtering test output from breadcrumbs
        options.AddBreadcrumbsForLogType = new Dictionary<LogType, bool>
        {
            { LogType.Error, true },
            { LogType.Assert, true },
            { LogType.Warning, true },
            { LogType.Log, false },
            { LogType.Exception, true },
        };

        // Disable ANR to avoid test interference
        options.DisableAnrIntegration();

        // Runtime initialization for integration tests
        options.AndroidNativeInitializationType = NativeInitializationType.Runtime;
        options.IosNativeInitializationType = NativeInitializationType.Runtime;

        Debug.Log("Sentry: IntegrationOptionsConfig::Configure() finished");
    }
}
