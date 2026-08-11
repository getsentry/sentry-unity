using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;

public class NativeButtons : MonoBehaviour
{
    private static int _afterNativeCall;

    [SerializeField] private Text _label;
    [SerializeField] private List<GameObject> _il2cppButtons;

    private void Start()
    {
#if !ENABLE_IL2CPP
        _label.color = Color.red;
        foreach (var il2CPPButton in _il2cppButtons)
        {
            il2CPPButton.GetComponent<Button>().interactable = false;
        }
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ForceCrash()
    {
        DoSomeWorkHere(() => Utils.ForceCrash(ForcedCrashCategory.AccessViolation));
        Interlocked.Increment(ref _afterNativeCall);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowCpp()
    {
        DoSomeWorkHere(throw_cpp);
        Interlocked.Increment(ref _afterNativeCall);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CrashInCpp()
    {
        DoSomeWorkHere(crash_in_cpp);
        Interlocked.Increment(ref _afterNativeCall);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CrashInC()
    {
        DoSomeWorkHere(crash_in_c);
        Interlocked.Increment(ref _afterNativeCall);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DoSomeWorkHere(Action action)
    {
        if (CheckSomeFakeWork())
        {
            DoSomeWorkThere(action);
            Interlocked.Increment(ref _afterNativeCall);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DoSomeWorkThere(Action action)
    {
        if (CheckSomeFakeWork())
        {
            action.Invoke();
            Interlocked.Increment(ref _afterNativeCall);
        }
    }

    // NoInlining ends up being inlined through L2CPP anyway. :(
    // We're checking some fake work here to prevent too aggressive optimization. That way, we can show off some proper
    // stack traces that are closer to real-world bugs and events.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckSomeFakeWork() => DateTime.Now.Ticks > 0; // Always true but not optimizable

    // CppPlugin.cpp
    [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
    private static extern void throw_cpp();
    [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
    private static extern void crash_in_cpp();

    // CPlugin.c
    [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
    private static extern void crash_in_c();

    public void CatchViaCallback() => call_into_csharp(new callback_t(csharpCallback));

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void callback_t(int code);

    [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
    private static extern void call_into_csharp(callback_t callback);

    // This method is called from the C library.
    [AOT.MonoPInvokeCallback(typeof(callback_t))]
    private static void csharpCallback(int code)
    {
        try
        {
            throw new Exception($"C# exception triggered via native callback. Code = {code}");
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }
}
