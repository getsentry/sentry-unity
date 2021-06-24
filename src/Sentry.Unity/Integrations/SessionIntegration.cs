using System;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Integrations
{
    public class SessionIntegration : ISdkIntegration
    {
        public SessionIntegration()
        { }

        public void Register(IHub hub, SentryOptions options)
        {
            if (!options.AutoSessionTracking)
            {
                return;
            }

            options.DiagnosticLogger?.LogDebug("Registering Session integration.");

            var gameListenerObject = new GameObject("SentryListener");
            gameListenerObject.hideFlags = HideFlags.HideAndDontSave;

            var gameListener = gameListenerObject.AddComponent<ApplicationPauseListener>();
            gameListener.ApplicationResuming += () =>
            {
                Debug.Log("resuming");
                hub.ResumeSession();
            };

            gameListener.ApplicationPausing += () =>
            {
                Debug.Log("pausing");
                hub.PauseSession();
            };

            gameListener.ApplicationQuitting += () =>
            {
                Debug.Log("quitting");
            };
        }
    }
}
