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
}
