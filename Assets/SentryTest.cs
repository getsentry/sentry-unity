using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using System;

public class SentryTest : MonoBehaviour
{
    new void SendMessage(String message)
    {
        if (message == "exception")
        {
            throw new DivideByZeroException();
        } else if (message == "assert") {
            Assert.AreEqual(message, "not equal");   
        }
    }
}
