using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// See https://docs.unity3d.com/2022.1/Documentation/Manual/handling-IL2CPP-additional-args.html
class Il2CppOption : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        string addlArgs = "--emit-source-mapping";
        UnityEngine.Debug.Log($"Setting Additional IL2CPP Args = \"{addlArgs}\" for platform {report.summary.platform}");
        PlayerSettings.SetAdditionalIl2CppArgs(addlArgs);
    }
}
