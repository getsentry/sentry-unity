using Sentry.Integrations;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    internal class SceneManagerIntegration : ISdkIntegration
    {
        private readonly ISceneManager _sceneManager;

        public SceneManagerIntegration() : this(SceneManagerAdapter.Instance)
        {
        }

        internal SceneManagerIntegration(ISceneManager sceneManager) => _sceneManager = sceneManager;

        public void Register(IHub hub, SentryOptions options)
        {
            options.DiagnosticLogger?.Log(SentryLevel.Debug, "Registering SceneManager integration.");

            _sceneManager.SceneLoaded += OnSceneManagerOnSceneLoaded;
            _sceneManager.SceneUnloaded += SceneManagerOnSceneUnloaded;
            _sceneManager.ActiveSceneChanged += SceneManagerOnActiveSceneChanged;

            void OnSceneManagerOnSceneLoaded(SceneAdapter scene, LoadSceneMode mode)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                hub.AddBreadcrumb(
                    message: $"Scene '{scene.Name}' was loaded",
                    category: "scene.loaded");
            }

            void SceneManagerOnSceneUnloaded(SceneAdapter scene)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                hub.AddBreadcrumb(
                    message: $"Scene '{scene.Name}' was unloaded",
                    category: "scene.unloaded");
            }

            void SceneManagerOnActiveSceneChanged(SceneAdapter fromScene, SceneAdapter toScene)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                hub.AddBreadcrumb(
                    message: fromScene.Name == null
                        ? $"Changed active scene to '{toScene.Name}'"
                        : $"Changed active scene '{fromScene.Name}' to '{toScene.Name}'",
                    category: "scene.changed");
            }
        }
    }
}
