using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Builds a player that only contains the screenshot performance harness.
    /// </summary>
    public static class ScreenshotPerfBuilder
    {
        // Generated on every build and ignored by git
        private const string ScenePath = "Assets/Scenes/Generated/" + ScreenshotPerfHarness.SceneName + ".unity";

        public static void Build()
        {
            var arguments = Environment.GetCommandLineArgs();
            var target = (BuildTarget)Enum.Parse(typeof(BuildTarget), GetArgument(arguments, "-buildTarget"), true);
            var outputPath = GetArgument(arguments, "-buildOutput");

            CreateScene();

            // Development builds are required for the profiler recorders to report script markers.
            // Frame timing stats provide the GPU frame time.
            var frameTimingStats = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;

            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    options = BuildOptions.Development
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new Exception($"Build failed with {report.summary.totalErrors} error(s).");
                }

                Debug.Log($"Screenshot performance build succeeded: {report.summary.outputPath}");
            }
            finally
            {
                PlayerSettings.enableFrameTimingStats = frameTimingStats;
                AssetDatabase.SaveAssets();
            }
        }

        private static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);

            // The harness bootstraps itself based on the scene name and builds its content at runtime
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(nameof(ScreenshotPerfHarness)).AddComponent<ScreenshotPerfHarness>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new Exception($"Failed to save the screenshot performance scene to {ScenePath}.");
            }
        }

        private static string GetArgument(string[] arguments, string name)
        {
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }

            throw new ArgumentException($"Required command line argument '{name}' was not provided.");
        }
    }
}
