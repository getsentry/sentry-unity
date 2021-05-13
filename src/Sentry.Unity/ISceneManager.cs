using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    internal interface ISceneManager
    {
        public event UnityAction<Scene, LoadSceneMode> SceneLoaded;
        public event UnityAction<Scene> SceneUnloaded;
        public event UnityAction<Scene, Scene> ActiveSceneChanged;
    }

    internal sealed class SceneManagerAdapter : ISceneManager
    {
        public event UnityAction<Scene, LoadSceneMode>? SceneLoaded;
        public event UnityAction<Scene>? SceneUnloaded;
        public event UnityAction<Scene, Scene>? ActiveSceneChanged;

        public static readonly SceneManagerAdapter Instance = new();

        private SceneManagerAdapter()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
            SceneManager.activeSceneChanged += ActiveSceneChanged;
        }
    }
}
