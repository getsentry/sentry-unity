using System;
using System.Globalization;
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

    // IL2CPP inlines this anyway :( - so we're adding some fake work to prevent the compiler from optimizing too much
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void StackTraceExampleB()
    {
        var someWork = DateTime.Now.ToString();
        if (someWork.Length > 0)  // This condition will always be true but compiler can't be certain
        {
            throw new InvalidOperationException("Exception from a lady beetle 🐞");
        }
    }

    // IL2CPP inlines this anyway :( - so we're adding some fake work to prevent the compiler from optimizing too much
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void StackTraceExampleA()
    {
        var someWork = DateTime.Now.ToString();
        if (someWork.Length > 0)  // This condition will always be true but compiler can't be certain
        {
            StackTraceExampleB();
        }
    }

    // IL2CPP inlines this anyway :( - so we're adding some fake work to prevent the compiler from optimizing too much
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogError()
    {
        var someWork = DateTime.Now.ToString();
        if (someWork.Length > 0)  // This condition will always be true but compiler can't be certain
        {
            Debug.LogError("Debug.LogError() called");
        }
    }
}

public class CustomException : Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
