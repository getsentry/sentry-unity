using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry.Unity
{
    // https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417
    public static class SentryInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (!File.Exists(SentryUnityOptions.GetConfigPath()))
            {
                Debug.LogWarning("Couldn't find the configuration file SentryOptions.json. Did you already configure Sentry?\nYou can do that through the editor: Tools -> Sentry");
                return;
            }

            var options = SentryUnityOptions.LoadFromUnity();

            if (!options.Enabled)
            {
                // We want to display the message in spite of how SentryOptions.json is configured
                new UnityLogger(SentryLevel.Info).Log(SentryLevel.Info, "Programmatic access enabled. Configure Sentry manually.");
                return;
            }

            if (!options.CaptureInEditor && Application.isEditor)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Info, "Disabled while in the Editor.");
                return;
            }

            if (string.IsNullOrWhiteSpace(options.Dsn))
            {
                options.DiagnosticLogger?.Log(SentryLevel.Warning, "No Sentry DSN configured. Sentry will be disabled.");
                return;
            }

            SentryUnity.Init(options);
        }
    }
}

