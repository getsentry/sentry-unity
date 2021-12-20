using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sentry;
using System;

public class CameraScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        try
        {
            //user code
            throw new Exception("User camera script failed");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
