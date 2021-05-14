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
                new UnityLogger(SentryLevel.Warning).Log(SentryLevel.Warning, "Sentry has not been configured. You can do that through the editor: Tools -> Sentry");
                return;
            }

            var options = SentryUnityOptions.LoadFromUnity();

            if (!options.Enabled)
            {
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

