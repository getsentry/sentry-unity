using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sentry;
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
        SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");
    }

    public void SetUser()
    {
        SentrySdk.ConfigureScope(s =>
        {
            s.User = new User
            {
                Email = "ant@farm.bug",
                Username = "ant",
                Id = "ant-id"
            };
        });
        Debug.Log("User set: ant");
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
#if !UNITY_EDITOR
        crash();
#else
        Debug.Log("Requires IL2CPP. Try this on a native player.");
#endif
    }

#if !UNITY_EDITOR
    // NativeExample.c
    [DllImport("__Internal")]
    private static extern void crash();
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodA() => throw new InvalidOperationException("Exception from A lady beetle 🐞");

    // IL2CPP inlines this anyway
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodB() => MethodA();
}

public class CustomException : System.Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
