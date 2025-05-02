using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;

namespace Sentry.Unity.Editor.WebGL;

public static class BuildPostProcess
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string executablePath)
    {
        var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
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
