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
                    $"Scene '{scene.Name}' was loaded",
                    category: "scene.loaded"
                    // TODO: What is worth paying the price of allocation in order to add here?
                    // data: new Dictionary<string, string>
                    // {
                    //     {"name", scene.Name},
                        // TODO: Should we benchmark before getting these? Are these and/or other unused fields useful?
                        // {"path", scene.path},
                        // {"isDirty", scene.isDirty.ToString()},
                    // }
                    // TODO: Is this useful? Does it happen that IsValid returns false at runtime?
                    // level: scene.IsValid()
                    //     ? BreadcrumbLevel.Error
                    //     : BreadcrumbLevel.Info
                );
            }

            void SceneManagerOnSceneUnloaded(SceneAdapter scene)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                hub.AddBreadcrumb(
                    $"Scene '{scene.Name}' was unloaded",
                    category: "scene.unloaded"
                    // data: new Dictionary<string, string>
                    // {
                    //     {"name", scene.Name},
                    // }
                );
            }

            void SceneManagerOnActiveSceneChanged(SceneAdapter fromScene, SceneAdapter toScene)
            {
                // In case Hub is disabled, avoid allocations below
                if (!hub.IsEnabled)
                {
                    return;
                }

                var message = $"Changed active scene '{fromScene.Name}' to '{toScene.Name}'";
                if (fromScene.Name == null)
                {
                    message = $"Changed active scene to '{toScene.Name}'";
                }

                hub.AddBreadcrumb(
                    message,
                    category: "scene.changed"
                );
            }
        }
    }
}
