using System;
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;

public class SentryTest : MonoBehaviour
{
    private int _counter;

    private void Update()
    {
        _counter++;
        if (_counter % 100 == 0) // every 100 frames
        {
            SentryRuntime.Instance.AddBreadcrumb("Frame number: " + _counter);
        }
    }

    private new void SendMessage(string message)
    {
        if (message == "exception")
        {
            throw new DivideByZeroException();
        }
        else if (message == "assert")
        {
            Assert.AreEqual(message, "not equal");
        }
        else if (message == "message")
        {
            SentryRuntime.Instance.CaptureMessage("this is a message");
        }
    }
}