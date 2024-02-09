using System;
using UnityEngine;

namespace Sentry.Unity
{
    public interface ISentryUnityInfo
    {
        public bool IL2CPP { get; }
        public Il2CppMethods? Il2CppMethods { get; }
        public bool IsKnownPlatform();
        public bool IsLinux();
        public bool IsNativeSupportEnabled(SentryUnityOptions options, RuntimePlatform platform);
        public bool IsSupportedBySentryNative(RuntimePlatform platform);
        public string GetDebugImageType(RuntimePlatform platform);
    }

    public class Il2CppMethods
    {
        public Il2CppMethods(
            Il2CppGcHandleGetTarget il2CppGcHandleGetTarget,
            Il2CppNativeStackTrace il2CppNativeStackTrace,
            Il2CppNativeStackTraceCurrentThread il2CppNativeStackTraceCurrentThread,
            Il2CppFree il2CppFree)
        {
            Il2CppGcHandleGetTarget = il2CppGcHandleGetTarget;
            Il2CppNativeStackTrace = il2CppNativeStackTrace;
            Il2CppNativeStackTraceCurrentThread = il2CppNativeStackTraceCurrentThread;
            Il2CppFree = il2CppFree;
        }

        public Il2CppGcHandleGetTarget Il2CppGcHandleGetTarget { get; }
        public Il2CppNativeStackTrace Il2CppNativeStackTrace { get; }
        public Il2CppNativeStackTraceCurrentThread Il2CppNativeStackTraceCurrentThread { get; }
        public Il2CppFree Il2CppFree { get; }
    }

    public delegate IntPtr Il2CppGcHandleGetTarget(IntPtr gchandle);
    public delegate void Il2CppNativeStackTrace(
        IntPtr exc,
        out IntPtr addresses,
        out int numFrames,
        out string? imageUUID,
        out string? imageName);

    public delegate void Il2CppNativeStackTraceCurrentThread(
        out IntPtr addresses,
        out int numFrames,
        out string? imageUUID,
        out string? imageName);
    public delegate void Il2CppFree(IntPtr ptr);
}
