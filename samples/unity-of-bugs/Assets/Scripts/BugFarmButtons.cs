using System;
using System.Runtime.CompilerServices;
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;

public class BugFarmButtons : MonoBehaviour
{
    private Threading _threading = Threading.MainThread;
    // private Dropdown _threadingDropdown;

    private void Start()
    {
        Debug.Log("Sample Start 🦋");
        Debug.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
        // _threadingDropdown = ;
        // _threadingDropdown.onValueChanged.AddListener(delegate
        // {
        //     DropdownValueChanged(m_Dropdown);
        // });

    }

    public void OnThreadingChange(Int32 value)
    {
        _threading = (Threading)Enum.ToObject(typeof(Threading), value);
        Debug.LogFormat("Switched threading to: {0} = {1}", value, _threading);
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
            SentrySdk.CaptureException(e);
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
            SentrySdk.CaptureException(e);
        }
    }

    public void CaptureMessage() => SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void StackTraceExampleB() => throw new InvalidOperationException("Exception from A lady beetle 🐞");

    // IL2CPP inlines this anyway :(
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void StackTraceExampleA() => StackTraceExampleB();
}

public class CustomException : Exception
{
    public CustomException(string message) : base(message)
    {
    }
}


internal enum Threading
{
    MainThread = 0,
    Task = 1,
    Coroutine = 2
}
