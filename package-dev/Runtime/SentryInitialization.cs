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
                SentryNativeIos.Configure(options);
#elif SENTRY_NATIVE_ANDROID
                var il2cpp =
#if ENABLE_IL2CPP
                true;
#else
                false;
#endif
                SentryNativeAndroid.Configure(options, il2cpp);
#endif

                SentryUnity.Init(options);
            }
        }
    }
}
