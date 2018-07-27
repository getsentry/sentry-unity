using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sentry;

public class SentryTest : MonoBehaviour {
    SentrySdk sentrySdk;

    public string DSNEntry;
    
	// Use this for initialization
	void Start () {
        sentrySdk = new SentrySdk(DSNEntry);
    }

    private void OnApplicationQuit()
    {
        //SentrySdk.Close();
    }

    new void SendMessage(String message)
    {
        try
        {
            // The following exception is captured and sent to Sentry
            throw new DivideByZeroException();
        }
        catch (Exception e)
        {
            sentrySdk.sendMessage("foo");   
            //Debug.Log("one");
            //SentrySdk.CaptureException(e);
            //Debug.Log("two");
        }
    }
}
