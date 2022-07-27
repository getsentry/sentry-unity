using Sentry;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;

public class NativeButtons : MonoBehaviour
{
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

    public void ForceCrash() => Utils.ForceCrash(ForcedCrashCategory.AccessViolation);

    public void ThrowCpp() => throw_cpp();

    public void CrashInCpp() => crash_in_cpp();

    public void CrashInC() => crash_in_c();

    // CppPlugin.cpp
    [DllImport("__Internal")]
    private static extern void throw_cpp();
    [DllImport("__Internal")]
    private static extern void crash_in_cpp();

    // CPlugin.c
    [DllImport("__Internal")]
    private static extern void crash_in_c();

    public void CatchViaCallback() => call_into_csharp(new callback_t(csharpCallback));

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void callback_t(int code);

    [DllImport("__Internal")]
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
