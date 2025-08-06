using UnityEngine;
using UnityEngine.Diagnostics;

public class ForceCrashButtons : MonoBehaviour
{
    public void AccessViolation() => Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
    public void FatalError() => Utils.ForceCrash(ForcedCrashCategory.FatalError);
    public void Abort() => Utils.ForceCrash(ForcedCrashCategory.Abort);
    public void PureVirtualFunction() => Utils.ForceCrash(ForcedCrashCategory.PureVirtualFunction);
    public void MonoAbort() => Utils.ForceCrash(ForcedCrashCategory.MonoAbort);
}
