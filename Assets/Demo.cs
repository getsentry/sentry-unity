using System.Collections;
using System.Collections.Generic;
using Sentry;
using UnityEngine;

public class Demo : MonoBehaviour
{
    private SentryBehavior _behavior;

    private void Start()
    {
        _behavior = GetComponent<SentryBehavior>();
    }

    public void Crash()
    {
        Debug.LogWarning("Crash");
        SentrySdk.Init("https://37c2358e61f041158167da7819b77b50@sentry.io/1306165");
    }
}
