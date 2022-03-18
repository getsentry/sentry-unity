#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#elif UNITY_STANDALONE_WIN && ENABLE_IL2CPP
#define SENTRY_NATIVE_WINDOWS
#endif
#endif

using UnityEngine;
using UnityEngine.Scripting;

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#elif UNITY_ANDROID
using Sentry.Unity.Android;
#elif SENTRY_NATIVE_WINDOWS
using Sentry.Unity.Native;
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
                var sentryUnityInfo = new SentryUnityInfo();

#if SENTRY_NATIVE_IOS
                SentryNativeIos.Configure(options);
#elif SENTRY_NATIVE_ANDROID
                SentryNativeAndroid.Configure(options, sentryUnityInfo);
#elif SENTRY_NATIVE_WINDOWS
                SentryNative.Configure(options);
#endif

                SentryUnity.Init(options);
            }
        }
    }

    public class SentryUnityInfo : ISentryUnityInfo
    {
        public bool IL2CPP
        {
            get =>
#if ENABLE_IL2CPP
               true;
#else
               false;
#endif
        }
    }
}
