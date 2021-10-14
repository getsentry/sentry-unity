using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    public void ThrowNull()
    {
        throw null;
    }

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

    public void CaptureMessage()
    {
        SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");
    }

    public void SetUser()
    {
        SentrySdk.ConfigureScope(s =>
        {
            s.User = new User
            {
                Email = "ant@farm.bug",
                Username = "ant",
                Id = "ant-id"
            };
        });
        Debug.Log("User set: ant");
    }

    public void RunOutOfMemory()
    {
        Debug.Log("Attempting to run out of memory");

        StartCoroutine(ConsumeMemory());

        // for (var i = 0; i < int.MaxValue; i++)
        // {
        //     memoryEaters[i] = new object();
        // }
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

    public void LoadNativeSupportScene() => SceneManager.LoadScene("2_MobileNativeSupport");
    public void LoadTransitionScene() => SceneManager.LoadScene("3_Transition");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodA() => throw new InvalidOperationException("Exception from A lady beetle 🐞");

    // IL2CPP inlines this anyway
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void MethodB() => MethodA();
}

public class CustomException : System.Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
