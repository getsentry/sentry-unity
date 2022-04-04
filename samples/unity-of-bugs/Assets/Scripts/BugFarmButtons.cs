using System;
using System.Runtime.CompilerServices;
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;

public class BugFarmButtons : MonoBehaviour, IBugFarm
{
    private IBugFarm _impl;

    private void Start()
    {
        Debug.Log("Sample Start 🦋");
        Debug.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
        OnThreadingChange(0);
    }

    public void OnThreadingChange(int value)
    {
        switch (value)
        {
            case 0:
                _impl = new BugFarmMainThread();
                break;
            default:
                throw new ArgumentException($"Invalid threading dropdown value: {value}");

        }
        Debug.LogFormat("Setting BugFarm implementation to: {0} = {1}", value, _impl.GetType());
    }

    public void AssertFalse() => _impl.AssertFalse();
    public void ThrowNull() => _impl.ThrowNull();
    public void ThrowExceptionAndCatch() => _impl.ThrowExceptionAndCatch();
    public void ThrowNullAndCatch() => _impl.ThrowNullAndCatch();
    public void CaptureMessage() => _impl.CaptureMessage();
    public void StackTraceExampleA() => _impl.StackTraceExampleA();
}

interface IBugFarm
{
    void AssertFalse();
    void ThrowNull();
    void ThrowExceptionAndCatch();
    void ThrowNullAndCatch();
    void CaptureMessage();
    void StackTraceExampleA();
}

internal class BugFarmMainThread : IBugFarm
{
    public void AssertFalse() => Assert.AreEqual(true, false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowNull() => throw null;

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
    private void StackTraceExampleB() => throw new InvalidOperationException("Exception from A lady beetle 🐞");

    // IL2CPP inlines this anyway :(
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void StackTraceExampleA() => StackTraceExampleB();
}

public class CustomException : Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
