using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace Sentry.Unity
{
    public static class SentryInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            Debug.Log("Trying to Initialize");
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options.ShouldInitializeSdk())
            {
                Debug.Log("Initializing");
                SentryUnity.Init(options);
            }
        }
    }
}
