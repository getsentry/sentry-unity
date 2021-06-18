using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity
{
    // https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417
    public static class SentryInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options == null)
            {
                new UnityLogger(new SentryOptions{DiagnosticLevel = SentryLevel.Warning}).LogWarning(
                    "Sentry has not been configured. You can do that through the editor: Tools -> Sentry");
                return;
            }

            if (!options.Enabled)
            {
                return;
            }

            if (!options.CaptureInEditor && Application.isEditor)
            {
                options.DiagnosticLogger?.LogInfo("Disabled while in the Editor.");
                return;
            }

            if (string.IsNullOrWhiteSpace(options.Dsn))
            {
                options.DiagnosticLogger?.LogWarning("No Sentry DSN configured. Sentry will be disabled.");
                return;
            }

            SentryUnity.Init(options);
        }
    }
}
