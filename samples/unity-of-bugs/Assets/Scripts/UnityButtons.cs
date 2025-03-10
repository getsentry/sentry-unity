using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using UnityEngine;
using UnityEngine.Diagnostics;

public class UnityButtons : MonoBehaviour
{
    private ForcedCrashCategory _selectedForcedCrashCategory;

    private void Start()
    {
    }

    public void OnCrashCategoryChange(int value)
    {
        string name;
        switch (value)
        {
            case 0:
                name = "AccessViolation";
                _selectedForcedCrashCategory = ForcedCrashCategory.AccessViolation;
                break;
            case 1:
                name = "FatalError";
                _selectedForcedCrashCategory = ForcedCrashCategory.FatalError;
                break;
            case 2:
                name = "Abort";
                _selectedForcedCrashCategory = ForcedCrashCategory.Abort;
                break;
            case 3:
                name = "PureVirtualFunction";
                _selectedForcedCrashCategory = ForcedCrashCategory.PureVirtualFunction;
                break;
            case 4:
                name = "MonoAbort";
                _selectedForcedCrashCategory = ForcedCrashCategory.MonoAbort;
                break;
            default:
                throw new ArgumentException($"Invalid forced-crash-type value: {value}");
        }

        Debug.Log($"Setting force-crash-type to: '{name}'");
    }

    public void ForceCrash() => Utils.ForceCrash(_selectedForcedCrashCategory);

    public void NativeAssert() => Utils.NativeAssert("Native Assert");

    public void NativeError() => Utils.NativeError("Native Error");
}
