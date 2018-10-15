using UnityEngine.Assertions;
using UnityEngine;
using System;

public class SentryTest : MonoBehaviour
{
    private int _counter = 0;

    private void Update()
    {
        _counter++;
        if (_counter % 100 == 0) // every 100 frames
        {
            SentrySdk.AddBreadcrumb("Frame number: " + _counter);
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
            SentrySdk.CaptureMessage("this is a message");
        }
    }
}
