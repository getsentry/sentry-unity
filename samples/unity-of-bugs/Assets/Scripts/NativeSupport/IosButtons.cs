using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PLATFORM_IOS
using System.Runtime.InteropServices;
#endif

public class IosButtons : MonoBehaviour
{
    public void ThrowObjectiveC()
    {
#if PLATFORM_IOS
        Debug.Log("The iOS SDK supports capturing Objective-C exceptions. Consider enabling 'GCC_ENABLE_OBJC_EXCEPTIONS' in the Xcode build settings.");
        throwObjectiveC();
#else
        Debug.Log("Requires IL2CPP. Try this on a native player that supports Objective-C native plugins.");
#endif
    }

#if PLATFORM_IOS
    // ObjectiveCPlugin.m
    [DllImport("__Internal")]
    private static extern void throwObjectiveC();
#endif

    public void RunOutOfMemory()
    {
        Debug.Log("Attempting to run out of memory");

        StartCoroutine(ConsumeMemory());
    }

    IEnumerator ConsumeMemory()
    {
        var memoryEaters = new List<object[]>();
        while(true)
        {
            yield return new WaitForEndOfFrame();
            memoryEaters.Add(new object[1000000]);
        }
    }

}
