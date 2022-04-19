using UnityEngine;
using System.Runtime.InteropServices;

public class WebGLButtons : MonoBehaviour
{
    public void ThrowJavaScript()
    {
#if PLATFORM_WEBGL
        throwJavaScript();
#else
        Debug.Log("Requires WebGL.");
#endif
    }

#if PLATFORM_WEBGL
    // JavaScriptPlugin.jslib
    [DllImport("__Internal")]
    private static extern void throwJavaScript();
#endif
}
