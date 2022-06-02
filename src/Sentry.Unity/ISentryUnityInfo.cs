using System;

namespace Sentry.Unity
{
    public interface ISentryUnityInfo
    {
        public bool IL2CPP { get; }
        public Il2CppMethods? Il2CppMethods { get; }
    }

    public class Il2CppMethods
    {
        public Il2CppMethods(
            Il2CppGcHandleGetTarget il2CppGcHandleGetTarget,
            Il2CppNativeStackTrace il2CppNativeStackTrace,
            Il2CppFree il2CppFree)
        {
            Il2CppGcHandleGetTarget = il2CppGcHandleGetTarget;
            Il2CppNativeStackTrace = il2CppNativeStackTrace;
            Il2CppFree = il2CppFree;
        }

        public Il2CppGcHandleGetTarget Il2CppGcHandleGetTarget { get; }
        public Il2CppNativeStackTrace Il2CppNativeStackTrace { get; }
        public Il2CppFree Il2CppFree { get; }
    }

    public delegate IntPtr Il2CppGcHandleGetTarget(int gchandle);
    public delegate void Il2CppNativeStackTrace(
        IntPtr exc,
        out IntPtr addresses,
        out int numFrames,
        out string? imageUUID,
        out string? imageName);
    public delegate void Il2CppFree(IntPtr ptr);
}
