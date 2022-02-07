using UnityEngine;

public class AndroidButtons : MonoBehaviour
{
    public void ThrowKotlin()
    {
#if UNITY_ANDROID
        using (var jo = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
        {
            jo.CallStatic("throw");
        }
#else
        Debug.LogWarning("Not running on Android.");
#endif
    }

    public void ThrowKotlinOnBackground()
    {
#if UNITY_ANDROID
        using (var jo = new AndroidJavaObject("unity.of.bugs.KotlinPlugin"))
        {
            jo.CallStatic("throwOnBackgroundThread");
        }
#else
        Debug.LogWarning("Not running on Android.");
#endif
    }
}
