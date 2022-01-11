#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#endif
#endif

using Sentry.Unity;
using UnityEngine;
using UnityEngine.Scripting;

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#elif UNITY_ANDROID
using Sentry.Unity.Android;
#endif

[assembly: AlwaysLinkAssembly]

internal static partial class SentryInitialization
{
    static partial void ConfigureOptions(SentryUnityOptions options);

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
#endif
            ConfigureOptions(options);
            SentryUnity.Init(options);
        }
    }
}
