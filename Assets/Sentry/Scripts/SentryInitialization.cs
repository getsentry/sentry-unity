using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry
{
    public static class SentryInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
//            if (Application.isEditor)
//            {
//                Debug.Log("Skipping initialization of Sentry since running in Unity editor");
//                return;
//            }
            Debug.Log("Initializing Sentry...");
            SentryRuntime.Instance.Initialize();
            Application.quitting += OnApplicationQuit;

            SentryRuntime.Instance.AddBreadcrumb(
                string.Format("BeforeSceneLoad {0}", SceneManager.GetActiveScene().name));

            Debug.Log("Sentry initialized");
        }

        private static void OnApplicationQuit()
        {
//            if (Application.isEditor)
//            {
//                return;
//            }

            Debug.Log("De-initializing Sentry...");
            SentryRuntime.Instance.Deinitialize();
            Debug.Log("Sentry de-initialized");
        }
    }
}
