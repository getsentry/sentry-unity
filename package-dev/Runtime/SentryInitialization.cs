using UnityEngine;
using UnityEngine.Scripting;

#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#endif
#endif

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

#if SENTRY_NATIVE_IOS
                options.ScopeObserver = new IosNativeScopeObserver(options);
                options.EnableScopeSync = true;
#elif SENTRY_NATIVE_ANDROID
                options.ScopeObserver = new UnityJavaScopeObserver(options);
                options.EnableScopeSync = true;
#endif

                SentryUnity.Init(options);
            }
        }
    }
}
