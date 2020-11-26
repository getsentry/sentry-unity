using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using Sentry;

#if UNITY_ANDROID
// using Sentry.Unity.Android;
#endif

public class SampleScript : MonoBehaviour
{
    public const bool IsIL2CPP
#if ENABLE_IL2CPP
        = true;
#else
        = false;
#endif

    private void Start()
    {
        Debug.Log("Starting sample script");

// #if UNITY_ANDROID
//         SentryAndroid.Init();
//         try {
//             SentryAndroid.TestThrow();
//         } catch {}
//         crash();
//         #else
        // TODO: from config
        // var dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
        // Sentry.Unity.SentryInitialization.Init(dsn);
// #endif
    }

    void Update()
    {
        // per frame
    }

    public void AssertFalse() => Assert.AreEqual(true, false);

    public void ThrowExceptionAndCatch()
    {
        Debug.Log("Throwing an instance of CustomException!");

        try
        {
            throw new CustomException("A custom exception.");
        }
        catch (Exception e)
        {
            SentrySdk.ConfigureScope(s =>
            {
                s.SetTag("development_build", Debug.isDebugBuild.ToString());
                s.SetTag("platform", Application.platform.ToString());
                s.SetTag("il2cpp", IsIL2CPP.ToString());
            });
            SentrySdk.CaptureException(e);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodA() => throw new Exception("exception from A");

    // IL2CPP inlines this anyway
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodB() => MethodA();

    public void ExceptionToString()
    {
        Debug.Log("Throw/Catch, Capture message Exception.ToString!");

        try
        {
            MethodB();
        }
        catch (Exception e)
        {
            SentrySdk.CaptureMessage($"typeof {e.GetType()}");
            SentrySdk.CaptureMessage($"StackTrace {e.StackTrace}");
            SentrySdk.CaptureMessage($"ToString {e}");
        }
    }

    public void ThrowNullAndCatch()
    {
        Debug.Log("Throwing 'null' and catching it!");

        try
        {
            MethodB();
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }

    public void SendMessage()
    {
        SentrySdk.CaptureMessage("Capturing message");
    }

    public void ThrowKotlin()
    {
#if UNITY_ANDROID
        var jo = new AndroidJavaObject("io.sentry.sample.unity.Buggy");
        jo.CallStatic("testThrow");
#else
        Debug.LogWarning("Not on Android.");
#endif
    }

    public void ThrowNull() => throw null;

    [DllImport("native")]
    private static extern void crash();
}

public class CustomException : System.Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
