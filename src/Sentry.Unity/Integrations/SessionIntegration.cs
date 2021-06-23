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
        private readonly ISceneManager _sceneManager;

        public SessionIntegration() : this(SceneManagerAdapter.Instance)
        {
        }

        internal SessionIntegration(ISceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            if (!options.AutoSessionTracking)
            {
                return;
            }

            options.DiagnosticLogger?.LogDebug("Registering Session integration.");

            _sceneManager.ActiveSceneChanged += CreateFocusListenerGameObject;
            void CreateFocusListenerGameObject(SceneAdapter fromScene, SceneAdapter toScene)
            {
                var gameListenerObject = new GameObject("SentryListener");
                gameListenerObject.hideFlags = HideFlags.HideInHierarchy;

                var gameListener = gameListenerObject.AddComponent<ApplicationPauseListener>();
                gameListener.ApplicationResuming += () =>
                {
                    hub.ResumeSession();
                };

                gameListener.ApplicationPausing += () =>
                {
                    hub.PauseSession();
                };
            }
        }
    }
}
