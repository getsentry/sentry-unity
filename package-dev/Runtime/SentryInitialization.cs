using UnityEngine;
using UnityEngine.Scripting;

#if UNITY_IOS && !UNITY_EDITOR
using Sentry.Unity.iOS;
#endif

[assembly: AlwaysLinkAssembly]

namespace Sentry.Unity
{
    public static class SentryInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options.ShouldInitializeSdk())
            {

#if UNITY_IOS && !UNITY_EDITOR
                options.ScopeObserver = new IosNativeScopeObserver(options);
                options.EnableScopeSync = true;
#elif UNITY_ANDROID && !UNITY_EDITOR
                options.ScopeObserver = new UnityJavaScopeObserver(options);
                options.EnableScopeSync = true;
#endif

                SentryUnity.Init(options);
            }
        }
    }
}
