using System;
using Sentry.Unity;
using UnityEngine;

public class SentryCliConfiguration : SentryCliOptionsConfiguration
{
    public override void Configure(SentryCliOptions cliOptions)
    {
        var isDevMachineWithSymbolUpload = Environment.GetEnvironmentVariable("DEV_SYMBOL_UPLOAD") == "true";
        if (isDevMachineWithSymbolUpload)
        {
            Debug.Log("'DEV_SYMBOL_UPLOAD' detected: Debug symbol upload has been enabled.");
            cliOptions.UploadSymbols = true;
        }
    }
}
