using System;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP || PLATFORM_IOS
using System.Runtime.InteropServices;
#endif
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class NativeSupportButtons : MonoBehaviour
{
    public void ThrowKotlin()
    {
#if UNITY_ANDROID
        using (var jo = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
        {
            jo.CallStatic("throw");
        }
#else
        Debug.LogWarning("Not running on Android.");
#endif
    }

    public void ThrowKotlinOnBackground()
    {
#if UNITY_ANDROID
        using (var jo = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
        {
            jo.CallStatic("throwOnBackgroundThread");
        }
#else
        Debug.LogWarning("Not running on Android.");
#endif
    }

    public void ThrowCpp()
    {
#if ENABLE_IL2CPP
        throw_cpp();
#else
        Debug.Log("Requires IL2CPP. Try this on a native player.");
#endif
    }

    public void CrashInCpp()
    {
#if ENABLE_IL2CPP
        crash_in_cpp();
#else
        Debug.Log("Requires IL2CPP. Try this on a native player.");
#endif
    }

    public void CrashInC()
    {
#if ENABLE_IL2CPP
        crash_in_c();
#else
        Debug.Log("Requires IL2CPP. Try this on a native player.");
#endif
    }

#if ENABLE_IL2CPP
    // CppPlugin.cpp
    [DllImport("__Internal")]
    private static extern void throw_cpp();
    [DllImport("__Internal")]
    private static extern void crash_in_cpp();

    // CPlugin.c
    [DllImport("__Internal")]
    private static extern void crash_in_c();
#endif

    public void ThrowObjectiveC()
    {
#if PLATFORM_IOS
        throwObjectiveC();
#else
        Debug.Log("Requires IL2CPP. Try this on a native player.");
#endif
    }

#if PLATFORM_IOS
    // ObjectiveCPlugin.m
    [DllImport("__Internal")]
    private static extern void throwObjectiveC();
#endif

    public void LoadBugfarm() => SceneManager.LoadScene("1_Bugfarm");
    public void LoadTransitionScene() => SceneManager.LoadScene("3_Transition");
}
