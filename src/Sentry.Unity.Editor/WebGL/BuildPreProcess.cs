using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor.WebGL;

internal class BuildPreProcess : IPreprocessBuildWithReport
{
    public int callbackOrder => 1;

    public void OnPreprocessBuild(BuildReport report)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(report.summary.platform);
        if (targetGroup is not BuildTargetGroup.WebGL)
        {
            return;
        }

        if (PlayerSettings.WebGL.exceptionSupport == WebGLExceptionSupport.None)
        {
            throw new BuildFailedException("WebGL exception support is set to None. The Sentry SDK requires exception " +
                "support to function properly. Please change the WebGL exception support setting in Player Settings " +
                "or disable the Sentry SDK.");
        }
    }
}
