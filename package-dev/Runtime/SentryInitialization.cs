#if !UNITY_EDITOR
#if UNITY_IOS || (UNITY_STANDALONE_OSX && ENABLE_IL2CPP)
#define SENTRY_NATIVE_COCOA
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#elif UNITY_64 && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX)
#define SENTRY_NATIVE
#elif UNITY_WEBGL
#define SENTRY_WEBGL
#endif
#endif

using System;
#if UNITY_2020_3_OR_NEWER
using System.Buffers;
using System.Runtime.InteropServices;
#endif
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
            var sentryUnityInfo = new SentryUnityInfo();
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions(sentryUnityInfo);
            if (options.ShouldInitializeSdk())
            {
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

        public string Platform
        {
            get =>
#if UNITY_IOS || UNITY_STANDALONE_OSX
                "macho"
#elif UNITY_ANDROID || UNITY_STANDALONE_LINUX
                "elf"
#elif UNITY_STANDALONE_WIN
                "pe"
#else
                "unknown"
#endif
            ;
        }

        public Il2CppMethods Il2CppMethods => _il2CppMethods;

        private Il2CppMethods _il2CppMethods
// Lowest supported version to have all required methods below
#if !ENABLE_IL2CPP || !UNITY_2020_3_OR_NEWER || !UNITY_64
            ;
#else
            = new Il2CppMethods(
                il2cpp_gchandle_get_target,
#if UNITY_2021_3_OR_NEWER
                il2cpp_native_stack_trace,
#else
                Il2CppNativeStackTraceShim,
#endif
                il2cpp_free);

        // Available in Unity `2019.4.34f1` (and later)
        // Il2CppObject* il2cpp_gchandle_get_target(uint32_t gchandle)
        [DllImport("__Internal")]
        private static extern IntPtr il2cpp_gchandle_get_target(int gchandle);

        // Available in Unity `2019.4.34f1` (and later)
        // void il2cpp_free(void* ptr)
        [DllImport("__Internal")]
        private static extern void il2cpp_free(IntPtr ptr);

#if UNITY_2021_3_OR_NEWER
#pragma warning disable 8632
        // Definition from Unity `2021.3` (and later):
        // void il2cpp_native_stack_trace(const Il2CppException * ex, uintptr_t** addresses, int* numFrames, char** imageUUID, char** imageName)
        [DllImport("__Internal")]
        private static extern void il2cpp_native_stack_trace(IntPtr exc, out IntPtr addresses, out int numFrames, out string? imageUUID, out string? imageName);
#pragma warning restore 8632
#else
#pragma warning disable 8632
        private static void Il2CppNativeStackTraceShim(IntPtr exc, out IntPtr addresses, out int numFrames, out string? imageUUID, out string? imageName)
        {
            imageName = null;
            // Unity 2020 does not *return* a newly allocated string as out-parameter, but rather expects a pre-allocated buffer it writes into.
            // That buffer needs to have space for the hex-encoded uuid (32) plus terminating nul-byte.
            var uuidBuffer = new char[32 + 1];
            il2cpp_native_stack_trace(exc, out addresses, out numFrames, uuidBuffer);
            // C-strings are nul-terminated, but the conversion here would normally keep that terminating nul-byte in the string, which we don't want.
            imageUUID = new string(uuidBuffer).TrimEnd('\0');
        }
#pragma warning restore 8632

        // Definition from Unity `2020.3`:
        // void il2cpp_native_stack_trace(const Il2CppException * ex, uintptr_t** addresses, int* numFrames, char* imageUUID)
        [DllImport("__Internal")]
        private static extern void il2cpp_native_stack_trace(IntPtr exc, out IntPtr addresses, out int numFrames, [Out] char[] imageUUID);
#endif

#endif
    }
}
