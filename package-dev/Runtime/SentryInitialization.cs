#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#elif UNITY_WEBGL
#define SENTRY_WEBGL
#endif
#endif

using UnityEngine;
using UnityEngine.Scripting;

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#elif UNITY_ANDROID
using Sentry.Unity.Android;
#elif SENTRY_WEBGL
using Sentry.Unity.WebGL;
#endif

// Remove me 
using Sentry.Unity.WebGL;

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
                SentryNativeAndroid.Configure(options);
#elif SENTRY_WEBGL
                SentryWebGL.Configure(options);
#endif

                SentryUnity.Init(options);
            }
        }
    }
}
