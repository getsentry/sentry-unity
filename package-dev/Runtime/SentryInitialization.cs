#if !UNITY_EDITOR
#if UNITY_IOS || (UNITY_STANDALONE_OSX && ENABLE_IL2CPP)
#define SENTRY_NATIVE_COCOA
#elif UNITY_ANDROID && ENABLE_IL2CPP
#define SENTRY_NATIVE_ANDROID
#elif UNITY_64 && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX)
#define SENTRY_NATIVE
#elif UNITY_GAMECORE
#define SENTRY_NATIVE
#elif UNITY_PS5
#define SENTRY_NATIVE
#elif UNITY_WEBGL
#define SENTRY_WEBGL
#endif
#endif

#if ENABLE_IL2CPP && (SENTRY_NATIVE_COCOA || SENTRY_NATIVE_ANDROID || SENTRY_NATIVE)
#define IL2CPP_LINENUMBER_SUPPORT
#endif

using System;
using Sentry.Unity;
using Sentry.Extensibility;
using Sentry.Unity.NativeUtils;
using System.Buffers;
using System.Runtime.InteropServices;
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
    internal static class SentryInitialization
    {
        /// <summary>
        /// This is intended for internal use only.
        /// The SDK relies on <c>SetupPlatformServices</c> getting called as the very first thing during the game's
        /// startup. This ensures that features like line number and native support are set up and configured properly.
        /// This is also the case when initializing manually from code.
        /// </summary>
#if SENTRY_WEBGL
        // On WebGL SubsystemRegistration is too early for the UnityWebRequestTransport and errors with 'URI empty'
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void Init()
        {
            // We're setting up platform specific configuration as the very first thing.
            // These are required to be available during initialization, i.e. UnityInfo and MainThreadData.
            SetUpPlatformServices();

            // Loading the options invokes the ScriptableOption`Configure` callback. Users can disable the SDK via code.
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options != null && options.ShouldInitializeSdk())
            {
                SentrySdk.Init(options);
            }
            else
            {
                // If the SDK is not `enabled` we're closing down the native layer as well. This is especially relevant
                // in a `built-time-initialization` scenario where the native SDKs self-initialize.
#if SENTRY_NATIVE_COCOA
                SentryNativeCocoa.Close(options);
#elif SENTRY_NATIVE_ANDROID
                SentryNativeAndroid.Close(options);
#endif
            }
        }

        private static void SetUpPlatformServices()
        {
            SentryPlatformServices.CollectMainThreadData();
            SentryPlatformServices.UnityInfo = new SentryUnityInfo();

#if SENTRY_NATIVE_COCOA
            SentryPlatformServices.PlatformConfiguration = SentryNativeCocoa.Configure;
#elif SENTRY_NATIVE_ANDROID
            SentryPlatformServices.PlatformConfiguration = SentryNativeAndroid.Configure;
#elif SENTRY_NATIVE
            SentryPlatformServices.PlatformConfiguration = SentryNative.Configure;
#elif SENTRY_WEBGL
            SentryPlatformServices.PlatformConfiguration = SentryWebGL.Configure;
#endif
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

        public Il2CppMethods Il2CppMethods => _il2CppMethods;

        private Il2CppMethods _il2CppMethods
            // Lowest supported version to have all required methods below
#if !IL2CPP_LINENUMBER_SUPPORT
            ;
#else
            = new Il2CppMethods(
                Il2CppGcHandleGetTargetShim,
                Il2CppNativeStackTraceShim,
                il2cpp_free);

#pragma warning disable 8632
        // The incoming `IntPtr` is a native `char*`, a pointer to a
        // nul-terminated C string. This function converts it to a C# string,
        // and also byte-swaps/truncates on ELF platforms.
        private static string? SanitizeDebugId(IntPtr debugIdPtr)
        {
            if (debugIdPtr == IntPtr.Zero)
            {
                return null;
            }

#if UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_PS5
            // For ELF platforms, the 20-byte `NT_GNU_BUILD_ID` needs to be
            // turned into a "little-endian GUID", which means the first three
            // components need to be byte-swapped appropriately.
            // See: https://getsentry.github.io/symbolicator/advanced/symbol-server-compatibility/#identifiers

            // We unconditionally byte-flip these as we assume that we only
            // ever run on little-endian platforms. Additionally, we truncate
            // this down from a 40-char build-id to a 32-char debug-id as well.
            SwapHexByte(debugIdPtr, 0, 3);
            SwapHexByte(debugIdPtr, 1, 2);
            SwapHexByte(debugIdPtr, 4, 5);
            SwapHexByte(debugIdPtr, 6, 7);
            Marshal.WriteByte(debugIdPtr, 32, 0);

            // This will swap the two hex-encoded bytes at offsets 1 and 2.
            // Internally, it treats these as Int16, as the hex-encoding means
            // they occupy 2 bytes each.
            void SwapHexByte(IntPtr buffer, Int32 offset1, Int32 offset2)
            {
                var a = Marshal.ReadInt16(buffer, offset1 * 2);
                var b = Marshal.ReadInt16(buffer, offset2 * 2);
                Marshal.WriteInt16(buffer, offset2 * 2, a);
                Marshal.WriteInt16(buffer, offset1 * 2, b);
            }

            // All other platforms we care about (Windows, macOS) already have
            // an appropriate debug-id format for that platform so no modifications
            // are needed.
#endif

            return Marshal.PtrToStringAnsi(debugIdPtr);
        }

#if UNITY_2023_1_OR_NEWER
        private static IntPtr Il2CppGcHandleGetTargetShim(IntPtr gchandle) => il2cpp_gchandle_get_target(gchandle);

        // Available in Unity `2013.3.12f1` (and later)
        // Il2CppObject* il2cpp_gchandle_get_target(Il2CppGCHandle gchandle)
        [DllImport("__Internal")]
        private static extern IntPtr il2cpp_gchandle_get_target(IntPtr gchandle);
#else
        private static IntPtr Il2CppGcHandleGetTargetShim(IntPtr gchandle) => il2cpp_gchandle_get_target(gchandle.ToInt32());

        // Available in Unity `2019.4.34f1` (and later)
        // Il2CppObject* il2cpp_gchandle_get_target(uint32_t gchandle)
        [DllImport("__Internal")]
        private static extern IntPtr il2cpp_gchandle_get_target(int gchandle);
#endif

        // Available in Unity `2019.4.34f1` (and later)
        // void il2cpp_free(void* ptr)
        [DllImport("__Internal")]
        private static extern void il2cpp_free(IntPtr ptr);

        private static void Il2CppNativeStackTraceShim(IntPtr exc, out IntPtr addresses, out int numFrames, out string? imageUUID, out string? imageName)
        {
            var uuidBuffer = IntPtr.Zero;
            var imageNameBuffer = IntPtr.Zero;
            il2cpp_native_stack_trace(exc, out addresses, out numFrames, out uuidBuffer, out imageNameBuffer);

            try
            {
                imageUUID = SanitizeDebugId(uuidBuffer);
                imageName = (imageNameBuffer == IntPtr.Zero) ? null : Marshal.PtrToStringAnsi(imageNameBuffer);
            }
            finally
            {
                il2cpp_free(uuidBuffer);
                il2cpp_free(imageNameBuffer);
            }
        }

        // Definition from Unity `2021.3` (and later):
        // void il2cpp_native_stack_trace(const Il2CppException * ex, uintptr_t** addresses, int* numFrames, char** imageUUID, char** imageName)
        [DllImport("__Internal")]
        private static extern void il2cpp_native_stack_trace(IntPtr exc, out IntPtr addresses, out int numFrames, out IntPtr imageUUID, out IntPtr imageName);

#pragma warning restore 8632
#endif
    }
}
