using UnityEngine;

namespace Sentry.Unity
{
    // https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417
    public static class SentryInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var options = UnitySentryOptions.LoadFromUnity();

            if (!options.DisableProgrammaticInitialization)
            {
                /*
                 * Call Unity's `Debug.Log()` directly via Sentry `UnityLogger`
                 * We want to display the message in spite of how SentryOptions.json is configured
                 */
                new UnityLogger(SentryLevel.Info).Log(SentryLevel.Info, "Programmatic access enabled. Configure Sentry manually.");
                return;
            }

            if (!options.Enabled)
            {
                options.DiagnosticLogger?.Log(SentryLevel.Debug, "Disabled In Options.");
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

