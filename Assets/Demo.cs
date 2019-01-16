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

    public void ThrowAndCatch()
    {
        Debug.Log("Throwing exception!");

        try
        {
            throw null;
        }
        catch (Exception e)
        {
            var evt = new SentryEvent(e);
            SentrySdk.CaptureEvent(evt);
        }
    }

    public new void SendMessage() => SentrySdk.CaptureMessage("Capturing message");
}
