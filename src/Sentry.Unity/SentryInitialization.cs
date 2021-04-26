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

            options.AddIntegration(new UnityApplicationLoggingIntegration(new EventCapture()));
            SentryUnity.Init(options);
        }
    }
}

