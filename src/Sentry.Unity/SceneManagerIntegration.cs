using System.Collections.Generic;
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
            options.DiagnosticLogger?.Log(SentryLevel.Debug, "Registering BeforeSceneLoad integration.");

            _sceneManager.SceneLoaded += OnSceneManagerOnSceneLoaded;
            _sceneManager.SceneUnloaded += SceneManagerOnSceneUnloaded;
            _sceneManager.ActiveSceneChanged += SceneManagerOnActiveSceneChanged;

            void OnSceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                hub.AddBreadcrumb(
                    $"Scene '{scene.name}' was loaded",
                    category: "scene.load",
                    // TODO: What is worth paying the price of allocation in order to add here?
                    data: new Dictionary<string, string>
                    {
                        {"name", scene.name},
                        {"path", scene.path},
                        {"isDirty", scene.isDirty.ToString()},
                        {"mode", mode.ToString()}
                    },
                    level: scene.IsValid()
                        ? BreadcrumbLevel.Error
                        : BreadcrumbLevel.Info);
            }

            void SceneManagerOnSceneUnloaded(Scene scene)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                hub.AddBreadcrumb(
                    $"Scene '{scene.name}' was unloaded",
                    category: "scene.unload",
                    data: new Dictionary<string, string>
                    {
                        {"name", scene.name},
                        {"path", scene.path},
                        {"isDirty", scene.isDirty.ToString()},
                    },
                    level: scene.IsValid()
                        ? BreadcrumbLevel.Error
                        : BreadcrumbLevel.Info);
            }

            void SceneManagerOnActiveSceneChanged(Scene fromScene, Scene toScene)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                hub.AddBreadcrumb(
                    $"Changed active scene '{fromScene.name}' to '{toScene.name}'",
                    category: "scene.changed");
            }
        }
    }
}
