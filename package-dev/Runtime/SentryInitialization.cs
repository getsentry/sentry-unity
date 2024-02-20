#if !UNITY_EDITOR
#if UNITY_IOS || (UNITY_STANDALONE_OSX && ENABLE_IL2CPP)
#define SENTRY_NATIVE_COCOA
#elif UNITY_ANDROID && ENABLE_IL2CPP
#define SENTRY_NATIVE_ANDROID
#elif UNITY_64 && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX)
#define SENTRY_NATIVE
#elif UNITY_WEBGL
#define SENTRY_WEBGL
#endif
#endif

#if ENABLE_IL2CPP && UNITY_2020_3_OR_NEWER && (SENTRY_NATIVE_COCOA || SENTRY_NATIVE_ANDROID || SENTRY_NATIVE)
#define IL2CPP_LINENUMBER_SUPPORT
#endif

using System;
using Sentry.Extensibility;
using AOT;
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
        public const string StartupTransactionOperation = "app.start";
        public static ISpan InitSpan;
        private const string InitSpanOperation = "runtime.init";
        public static ISpan SubSystemRegistrationSpan;
        private const string SubSystemSpanOperation = "runtime.init.subsystem";

#if SENTRY_WEBGL
        // On WebGL SubsystemRegistration is too early for the UnityWebRequestTransport and errors with 'URI empty'
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void Init()
        {
            var sentryUnityInfo = new SentryUnityInfo();
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions(sentryUnityInfo);
            if (options != null && options.ShouldInitializeSdk())
            {
                SentryIntegrations.Configure(options);
                Exception nativeInitException = null;

                try
                {
#if SENTRY_NATIVE_COCOA
                    SentryNativeCocoa.Configure(options, sentryUnityInfo);
#elif SENTRY_NATIVE_ANDROID
                    SentryNativeAndroid.Configure(options, sentryUnityInfo);
#elif SENTRY_NATIVE
                    SentryNative.Configure(options, sentryUnityInfo);
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

#if !SENTRY_WEBGL
                if (options.TracesSampleRate > 0.0f && options.AutoStartupTraces)
                {
                    options.DiagnosticLogger?.LogInfo("Creating '{0}' transaction for runtime initialization.",
                        StartupTransactionOperation);

                    var runtimeStartTransaction =
                        SentrySdk.StartTransaction("runtime.initialization", StartupTransactionOperation);
                    SentrySdk.ConfigureScope(scope => scope.Transaction = runtimeStartTransaction);

                    options.DiagnosticLogger?.LogDebug("Creating '{0}' span.", InitSpanOperation);
                    InitSpan = runtimeStartTransaction.StartChild(InitSpanOperation, "runtime initialization");
                    options.DiagnosticLogger?.LogDebug("Creating '{0}' span.", SubSystemSpanOperation);
                    SubSystemRegistrationSpan = InitSpan.StartChild(SubSystemSpanOperation, "subsystem registration");
                }
#endif
            }
            else
            {
                // Closing down the native layer that are set up during build and self-initialize
#if SENTRY_NATIVE_COCOA
                SentryNativeCocoa.Close(options.DiagnosticLogger);
#elif SENTRY_NATIVE_ANDROID
                SentryNativeAndroid.Close(options.DiagnosticLogger);
#endif
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

        public Il2CppMethods Il2CppMethods => _il2CppMethods;

        private Il2CppMethods _il2CppMethods
            // Lowest supported version to have all required methods below
#if !IL2CPP_LINENUMBER_SUPPORT
            ;
#else
            = new Il2CppMethods(
                Il2CppGcHandleGetTargetShim,
                Il2CppNativeStackTraceShim,
                Il2CppNativeStackTraceCurrentThreadShim,
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

#if UNITY_ANDROID || UNITY_STANDALONE_LINUX
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

#if UNITY_2023
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

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppStackFrameInfo
        {
            public IntPtr method;
            public IntPtr raw_ip;

            public int sourceCodeLineNumber;
            public int ilOffset;

            public IntPtr filePath;
        }

        private static void Il2CppNativeStackTraceCurrentThreadShim(out IntPtr addresses, out int numFrames, out string? imageUUID, out string? imageName)
        {
            // Currently there is no obvious way to obtain image UUID and name
            var uuidBuffer = IntPtr.Zero;
            var imageNameBuffer = IntPtr.Zero;

            numFrames = il2cpp_current_thread_get_stack_depth();

            var frames = new IntPtr[numFrames];

            for(int i = 0; i < numFrames; ++i)
            {
                IntPtr farameInfoPtr = IntPtr.Zero;
                try
                {
                    farameInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppStackFrameInfo>());

                    // Is it reliable to query current stack trace here?
                    var res = il2cpp_current_thread_get_frame_at(i, farameInfoPtr);
                    if(res)
                    {
                        Il2CppStackFrameInfo frameInfo = Marshal.PtrToStructure<Il2CppStackFrameInfo>(farameInfoPtr);
                        frames[i] = frameInfo.raw_ip;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(farameInfoPtr);
                }
            }

            int size = Marshal.SizeOf(typeof(IntPtr)) * numFrames;
            addresses = Marshal.AllocHGlobal(size);

            Marshal.Copy(frames, 0, addresses, numFrames);

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

        [DllImport("__Internal")]
        private static extern int il2cpp_current_thread_get_stack_depth();

        [DllImport("__Internal")]
        private static extern bool il2cpp_current_thread_get_frame_at(int offset, IntPtr frame);

#if UNITY_2021_3_OR_NEWER
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
#else
        private static void Il2CppNativeStackTraceShim(IntPtr exc, out IntPtr addresses, out int numFrames, out string? imageUUID, out string? imageName)
        {
            imageName = null;
            // Unity 2020 does not *return* a newly allocated string as out-parameter,
            // but rather expects a pre-allocated buffer it writes into.
            // That buffer needs to have space for either:
            // - A hex-encoded `LC_UUID` on MacOS (32)
            // - A hex-encoded GUID + Age on Windows (40)
            // - A hex-encoded `NT_GNU_BUILD_ID` on ELF (Android/Linux) (40)
            // plus a terminating nul-byte.
            var uuidBuffer = il2cpp_alloc(40 + 1);
            il2cpp_native_stack_trace(exc, out addresses, out numFrames, uuidBuffer);

            try
            {
                imageUUID = SanitizeDebugId(uuidBuffer);
            }
            finally
            {
                il2cpp_free(uuidBuffer);
            }
        }

        // Available in Unity `2020.3` (possibly even sooner)
        // void* il2cpp_alloc(size_t size)
        [DllImport("__Internal")]
        private static extern IntPtr il2cpp_alloc(uint size);

        // Definition from Unity `2020.3`:
        // void il2cpp_native_stack_trace(const Il2CppException * ex, uintptr_t** addresses, int* numFrames, char* imageUUID)
        [DllImport("__Internal")]
        private static extern void il2cpp_native_stack_trace(IntPtr exc, out IntPtr addresses, out int numFrames, IntPtr imageUUID);
#endif
#pragma warning restore 8632
#endif

        public bool IsKnownPlatform()
        {
            var platform = Application.platform;
            return
					platform == RuntimePlatform.Android ||
                   	platform == RuntimePlatform.IPhonePlayer ||
                   	platform == RuntimePlatform.WindowsEditor ||
                   	platform == RuntimePlatform.WindowsPlayer ||
                   	platform == RuntimePlatform.OSXEditor ||
                   	platform == RuntimePlatform.OSXPlayer ||
                   	platform == RuntimePlatform.LinuxEditor ||
                   	platform == RuntimePlatform.LinuxPlayer ||
					platform == RuntimePlatform.WebGLPlayer
#if UNITY_2021_3_OR_NEWER
                   	||
				   	platform == RuntimePlatform.WindowsServer ||
					platform == RuntimePlatform.OSXServer ||
                   	platform == RuntimePlatform.LinuxServer
#endif
                ;
        }

        public bool IsLinux()
        {
            var platform = Application.platform;
            return
                platform == RuntimePlatform.LinuxPlayer
#if UNITY_2021_3_OR_NEWER
                   	|| platform == RuntimePlatform.LinuxServer
#endif
                ;
        }

		public bool IsNativeSupportEnabled(SentryUnityOptions options, RuntimePlatform platform)
		{
            switch (platform)
            {
				case RuntimePlatform.Android:
                    return options.AndroidNativeSupportEnabled;
                case RuntimePlatform.IPhonePlayer:
                    return options.IosNativeSupportEnabled;
                case RuntimePlatform.WindowsPlayer:
                    return options.WindowsNativeSupportEnabled;
                case RuntimePlatform.OSXPlayer:
                    return options.MacosNativeSupportEnabled;
                case RuntimePlatform.LinuxPlayer:
                    return options.LinuxNativeSupportEnabled;
#if UNITY_2021_3_OR_NEWER
                case RuntimePlatform.WindowsServer:
                    return options.WindowsNativeSupportEnabled;
                case RuntimePlatform.OSXServer:
                    return options.MacosNativeSupportEnabled;
                case RuntimePlatform.LinuxServer:
                    return options.LinuxNativeSupportEnabled;
#endif
                default:
                    return false;
            }
        }


        public bool IsSupportedBySentryNative(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.Android
                   || platform == RuntimePlatform.LinuxPlayer
                   || platform == RuntimePlatform.WindowsPlayer
#if UNITY_2021_3_OR_NEWER
                   || platform == RuntimePlatform.WindowsServer
                   || platform == RuntimePlatform.OSXServer
                   || platform == RuntimePlatform.LinuxServer
#endif
                ;
        }

        public string GetDebugImageType(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "elf";
                case RuntimePlatform.IPhonePlayer:
                    return "macho";
                case RuntimePlatform.OSXPlayer:
                    return "macho";
                case RuntimePlatform.LinuxPlayer:
                    return "elf";
                case RuntimePlatform.WindowsPlayer:
                    return "pe";
#if UNITY_2021_3_OR_NEWER
                case RuntimePlatform.WindowsServer:
                    return "pe";
                case RuntimePlatform.OSXServer:
                    return "macho";
                case RuntimePlatform.LinuxServer:
                    return "elf";
#endif
                default:
                    return "unknown";
            }
        }
    }
}
