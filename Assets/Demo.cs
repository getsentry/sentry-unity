using System;
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;

public class Demo : MonoBehaviour
{
    private void Start()
    {
        SentrySdk.AddBreadcrumb("Demo starting!");

        Debug.LogWarning("Unity Debug.LogWarning calls are stored as breadcrumbs.");
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

    public void SendMessage() => SentrySdk.CaptureMessage("Capturing message");

    private class CustomException : Exception
    {
        public CustomException(string message) : base(message)
        {
        }
    }
}
