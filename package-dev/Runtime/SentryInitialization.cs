#if !UNITY_EDITOR
#if UNITY_IOS || (UNITY_STANDALONE_OSX && ENABLE_IL2CPP)
#define SENTRY_NATIVE_COCOA
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
#define SENTRY_NATIVE
#elif UNITY_WEBGL
#define SENTRY_WEBGL
#else
#define SENTRY_DEFAULT
#endif
#endif

using System;
using UnityEngine;
using UnityEngine.Scripting;

#if SENTRY_NATIVE_COCOA
using Sentry.Unity.iOS;
#elif SENTRY_NATIVE_ANDROID
using Sentry.Unity.Android;
#elif SENTRY_NATIVE
using Sentry.Unity.Native;
#elif SENTRY_WEBGL
using Sentry.Unity.WebGL;
#elif SENTRY_DEFAULT
using Sentry.Unity.Default;
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

                Exception nativeInitException = null;

                try
                {
#if SENTRY_NATIVE_COCOA
                    SentryNativeCocoa.Configure(options, sentryUnityInfo);
#elif SENTRY_NATIVE_ANDROID
                    SentryNativeAndroid.Configure(options, sentryUnityInfo);
#elif SENTRY_NATIVE
                    SentryNative.Configure(options);
#elif SENTRY_WEBGL
                    SentryWebGL.Configure(options);
#elif SENTRY_DEFAULT
                    SentryUnknownPlatform.Configure(options);
#endif
                }
                catch (DllNotFoundException e)
                {
                    nativeInitException = new Exception(
                        "Sentry native-error capture configuration failed to load a native library. This usually " +
                        "means the library is missing from the application bundle or the installation directory.", e);
                }
                catch (Exception e)
                {
                    nativeInitException = new Exception("Sentry native error capture configuration failed.", e);
                }

                SentryUnity.Init(options);
                if (nativeInitException != null)
                {
                    SentrySdk.CaptureException(nativeInitException);
                }
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
