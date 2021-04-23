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
                options.Logger?.Log(SentryLevel.Debug, "Disabled In Options.");
                return;
            }

            if (!options.CaptureInEditor && Application.isEditor)
            {
                options.Logger?.Log(SentryLevel.Info, "Disabled while in the Editor.");
                return;
            }

            if (string.IsNullOrWhiteSpace(options.Dsn))
            {
                options.Logger?.Log(SentryLevel.Warning, "No Sentry DSN configured. Sentry will be disabled.");
                return;
            }

            SentryUnity.Init(options);

            /*SentryUnity.Init(new UnitySentryOptions
            {
                Enabled = true,
                Dsn = "https://b8fd848b31444e80aa102e96d2a6a648@o510466.ingest.sentry.io/5606182"
            });*/
        }
    }
}

