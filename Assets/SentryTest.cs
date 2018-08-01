using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SentryTest : MonoBehaviour
{
    new void SendMessage(String message)
    {
        throw new DivideByZeroException();
    }
}
