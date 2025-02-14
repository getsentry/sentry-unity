#if UNITY_ANDROID && !UNITY_2020_1_OR_NEWER
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// With version v8 of the sentry-java SDK the min-sdk-version got raised to 21
    /// The default min-version for Unity 2019 builds is 19
    /// </summary>
    public class GradleProjectUpdater : MonoBehaviour
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuildProject)
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21;
        }
    }
}
#endif
