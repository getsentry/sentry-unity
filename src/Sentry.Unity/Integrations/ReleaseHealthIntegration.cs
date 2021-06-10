using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity.Integrations
{
    public class ReleaseHealthIntegration : ISdkIntegration
    {
        private readonly ISceneManager _sceneManager;

        public ReleaseHealthIntegration() : this(SceneManagerAdapter.Instance)
        {
        }

        internal ReleaseHealthIntegration(ISceneManager sceneManager) => _sceneManager = sceneManager;

        public void Register(IHub hub, SentryOptions options)
        {
            options.DiagnosticLogger?.Log(SentryLevel.Debug, "Registering SceneManager integration.");

            _sceneManager.ActiveSceneChanged += CreateEventListener;
        }

        private void CreateEventListener(SceneAdapter fromScene, SceneAdapter toScene)
        {
            var gameListenerObject = new GameObject("SentryListener");
            gameListenerObject.hideFlags = HideFlags.HideInHierarchy;

            var gameListener = gameListenerObject.AddComponent<GameEventListener>();
            gameListener.ApplicationPause += OnApplicationPaused;
            gameListener.ApplicationFocus += OnApplicationFocus;
        }

        private void OnApplicationPaused(bool isPaused)
        {
            // Session magic here
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Session magic here
        }
    }
}
