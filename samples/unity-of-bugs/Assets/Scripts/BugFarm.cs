using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

public class BugFarm : MonoBehaviour
{
    public const bool IsIL2CPP
#if ENABLE_IL2CPP
        = true;
#else
        = false;
#endif

    private void Start()
    {
        Debug.Log("Sample Start 🦋");
        Debug.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
    }

    void Update()
    {
    }

    public void AssertFalse() => Assert.AreEqual(true, false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowNull()
    {
        throw null;
    }

    public void ThrowExceptionAndCatch()
    {
        Debug.Log("Throwing an instance of 🐛 CustomException!");

        try
        {
            throw new CustomException("Custom bugs 🐛🐛🐛🐛.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void ThrowNullAndCatch()
    {
        Debug.Log("Throwing 'null' and catching 🐜🐜🐜 it!");

        try
        {
            ThrowNull();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void SendMessage()
    {
        Debug.Log("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");
    }

    public void ExceptionToString()
    {
        Debug.Log("Throw/Catch, Debug.LogError: Exception.ToString!");

        try
        {
            MethodB();
        }
        catch (Exception e)
        {
            Debug.LogError($"ExceptionToString:\n{e}");
        }
    }

    public void ThrowKotlin()
    {
#if UNITY_ANDROID
        var jo = new AndroidJavaObject("unity.of.bugs.KotlinPlugin");
        jo.CallStatic("throw");
#else
        Debug.LogWarning("Not on Android.");
#endif
    }

    public void ThrowKotlinOnBackground()
    {
#if UNITY_ANDROID
        var jo = new AndroidJavaObject("unity.of.bugs.KotlinPlugin");
        jo.CallStatic("throwOnBackgroundThread");
#else
        Debug.LogWarning("Not on Android.");
#endif
    }

    public void CrashNative()
    {
        // The method definition is missing
        //crash();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodA() => throw new InvalidOperationException("Exception from A lady beetle 🐞");

    // IL2CPP inlines this anyway
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodB() => MethodA();

    // The method definition is missing
    //[DllImport("__Internal")]
    //private static extern void crash();
}

public class CustomException : System.Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
