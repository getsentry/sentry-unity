#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#endif
#endif

using UnityEngine;
using UnityEngine.Scripting;

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#elif UNITY_ANDROID
using Sentry.Unity.Android;
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
                if (options.IosNativeSupportEnabled)
                {
                    options.ScopeObserver = new IosNativeScopeObserver(options);
                    options.EnableScopeSync = true;
                }
#elif SENTRY_NATIVE_ANDROID
                if (options.AndroidNativeSupportEnabled)
                {
                    options.ScopeObserver = new UnityJavaScopeObserver(options);
                    options.EnableScopeSync = true;
                    SentryNative.ReinstallBackend();
                }
#endif

                SentryUnity.Init(options);
            }
        }
    }
}
