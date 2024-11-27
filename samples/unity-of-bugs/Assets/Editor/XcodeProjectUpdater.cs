#if UNITY_IOS && !UNITY_2021_1_OR_NEWER
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Unity adds the '-mno-thumb' compiler flag by default to iOS builds, but this flag is outdated
    /// and no longer supported by modern versions of Xcode. Leaving this flag in the project will cause
    /// Xcode builds to fail with an "unsupported option '-mno-thumb' for target" error. This post-process 
    /// build step removes the flag to ensure the sample works on all versions of Unity.
    /// </summary>
    public static class XcodeProjectUpdater
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuildProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            Debug.Log("XcodeUpdater.OnPostProcessBuild started.");

            var pbxProjectPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
            var project = new PBXProject();
            project.ReadFromFile(pbxProjectPath);

            var targetGuid = project.GetUnityMainTargetGuid();

            Debug.Log("Removing '-mno-thumb' from 'OTHER_CFLAGS'.");
            project.UpdateBuildProperty(targetGuid, "OTHER_CFLAGS", null, new[] { "-mno-thumb" });

            project.WriteToFile(pbxProjectPath);

            Debug.Log("XcodeUpdater.OnPostProcessBuild finished.");
        }
    }
}
#endif
