using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID
using Sentry.Unity.Android;
#endif

public class SampleScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ThrowNull()
    {
#if UNITY_ANDROID
        Debug.Log("Sentry SDK for Android.");
        SentryAndroid.Init();
        Debug.Log("Initialized. Now going to throw test.");
        SentryAndroid.TestThrow();
#else
        throw null;
#endif
    }
}
