using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Sentry;

#if UNITY_ANDROID
using Sentry.Unity.Android;
#endif

public class SampleScript : MonoBehaviour
{
    private void Start()
    {
        SentrySdk.AddBreadcrumb("Demo starting!");

        Debug.LogWarning("Unity Debug.LogWarning calls are stored as breadcrumbs.");
    }

    void Update()
    {
        // per frame
    }

    public void AssertFalse() => Assert.AreEqual(true, false);

    public void ThrowUnhandled() => throw null;

    public void ThrowExceptionAndCatch()
    {
        Debug.Log("Throwing an instance of CustomException!");

        try
        {
            throw new CustomException("A custom exception.");
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }

    public void ThrowNullAndCatch()
    {
        Debug.Log("Throwing 'null' and catching it!");

        try
        {
            throw null;
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

    private void ThrowKotlin()
    {
#if UNITY_ANDROID
        var jo = new AndroidJavaObject("io.sentry.sample.unity.Buggy");
        jo.CallStatic("testThrow");
#endif
    }

    public void ThrowNull()
    {
#if UNITY_ANDROID
        Debug.Log("Sentry SDK for Android.");
        SentryAndroid.Init();
        Debug.Log("Initialized. Now going to throw test.");
        try {
        SentryAndroid.TestThrow();
        } catch {}
        crash();
#else
        throw null;
#endif
    }

    [DllImport("native")]
    private static extern void crash();
}

public class CustomException : System.Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
