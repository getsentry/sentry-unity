using UnityEngine;

namespace Sentry.Unity.Android;

public interface IAndroidJNI
{
    public void AttachCurrentThread();
    public void DetachCurrentThread();
    public int GetVersion();
}

public class AndroidJNIAdapter : IAndroidJNI
{
    public static readonly AndroidJNIAdapter Instance = new();

    public void AttachCurrentThread() => AndroidJNI.AttachCurrentThread();

    public void DetachCurrentThread() => AndroidJNI.DetachCurrentThread();
    public int GetVersion() => AndroidJNI.GetVersion();
}
