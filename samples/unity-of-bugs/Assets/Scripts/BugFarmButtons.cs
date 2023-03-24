using System;
using System.Data;
using System.Runtime.CompilerServices;
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;

public class BugFarmButtons : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("Sample 🐛");
    }

    private void Start()
    {
        Debug.Log("Sample Start 🦋");
        Debug.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        var e = 6;
        Demo(a, b, c, d, e);
    }

    public void Demo(int a, int b, int c, int d, int e)
    {
        throw new Exception((a + b + c + d + e).ToString());
    }

    public void AssertFalse() => Assert.AreEqual(true, false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowNull() => throw new NullReferenceException();

    public void ThrowExceptionAndCatch()
    {
        Debug.Log("Throwing an instance of 🐛 CustomException!");

        try
        {
            throw new CustomException("Custom bugs 🐛🐛🐛🐛.");
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
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
            SentrySdk.CaptureException(e);
        }
    }

    public void CaptureMessage() => SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void StackTraceExampleB() => throw new InvalidOperationException("Exception from a lady beetle 🐞");

    // IL2CPP inlines this anyway :(
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void StackTraceExampleA() => StackTraceExampleB();

    public void LogError() => Debug.LogError("Debug.LogError() called");
}

public class CustomException : Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
