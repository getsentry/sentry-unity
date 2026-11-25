using System.Runtime.InteropServices;

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// The native plugins' functions: CPlugin.c, CppPlugin.cpp and ObjectiveCPlugin.m.
    /// IL2CPP links them into the player, so the C and C++ ones need an IL2CPP build.
    /// </summary>
    internal static class NativeBridge
    {
        // CPlugin.c
        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void crash_in_c();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void callback_t(int code);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void call_into_csharp(callback_t callback);

        // CppPlugin.cpp
        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void throw_cpp();

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void crash_in_cpp();

#if UNITY_IOS && !UNITY_EDITOR
        // ObjectiveCPlugin.m
        [DllImport("__Internal")]
        internal static extern void throwObjectiveC();

        [DllImport("__Internal")]
        internal static extern void applicationNotResponding();
#endif
    }
}
