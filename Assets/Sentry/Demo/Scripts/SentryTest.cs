using UnityEngine.Assertions;
using UnityEngine;
using System;

public class SentryTest : MonoBehaviour
{
    public int counter = 0;

    private void Update()
    {
        counter++;
        if (counter % 100 == 0) // every 100 frames
        {
            SentrySdk.addBreadcrumb("Frame number: " + counter);
        }
    }

    private new void SendMessage(String message)
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
