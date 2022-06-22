using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal class Il2CppOption : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var arguments = "--emit-source-mapping";
            Debug.Log($"Setting additional IL2CPP arguments = '{arguments}' for platform {report.summary.platform}");
            PlayerSettings.SetAdditionalIl2CppArgs(arguments);
        }
    }
}
