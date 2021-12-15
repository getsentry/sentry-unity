using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#if ENABLE_IL2CPP || PLATFORM_IOS
using System.Runtime.InteropServices;
#endif
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class BugFarmButtons : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Sample Start 🦋");
        Debug.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
    }

    public void AssertFalse() => Assert.AreEqual(true, false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowNull() => throw null;

    public void ThrowExceptionAndCatch()
    {
        Debug.Log("Throwing an instance of 🐛 CustomException!");

        try
        {
            throw new CustomException("Custom bugs 🐛🐛🐛🐛.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void ThrowNullAndCatch()
    {
        Debug.Log("Throwing 'null' and catching 🐜🐜🐜 it!");

        try
        {
            ThrowNull();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void CaptureMessage() => SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");

    public void LoadNativeSupportScene() => SceneManager.LoadScene("2_MobileNativeSupport");
    public void LoadAdditionalSamplesScene() => SceneManager.LoadScene("3_AdditionalSamples");
    public void LoadUnityScene() => SceneManager.LoadScene("4_Unity");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void StackTraceExampleB() => throw new InvalidOperationException("Exception from A lady beetle 🐞");

    // IL2CPP inlines this anyway :(
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void StackTraceExampleA() => StackTraceExampleB();
}

public class CustomException : System.Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
