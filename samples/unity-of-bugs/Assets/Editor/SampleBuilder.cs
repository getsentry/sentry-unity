using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor
{
    public static class SampleBuilder
    {
        /// <summary>
        /// Builds the player for <c>-buildTarget</c> into <c>-buildOutput</c>. For iOS, <c>-iosSdk Device|Simulator</c>
        /// picks the SDK the Xcode project builds against: a phone by default. The project's own setting is put back
        /// afterwards, so a simulator build leaves ProjectSettings unchanged.
        /// </summary>
        public static void Build()
        {
            var arguments = Environment.GetCommandLineArgs();
            var target = GetBuildTarget(GetArgument(arguments, "-buildTarget"));
            var outputPath = GetArgument(arguments, "-buildOutput");
            var scenes = GetEnabledScenes();

            var projectSdk = PlayerSettings.iOS.sdkVersion;
            if (target == BuildTarget.iOS)
            {
                SetIosSdk(FindArgument(arguments, "-iosSdk") ?? "Device");
            }

            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = target,
                    targetGroup = GetBuildTargetGroup(target)
                });
            }
            finally
            {
                PlayerSettings.iOS.sdkVersion = projectSdk;
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed with {report.summary.totalErrors} error(s).");
            }

            Debug.Log($"Build succeeded: {report.summary.outputPath} ({report.summary.totalSize} bytes)");
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            if (scenes.Count == 0)
            {
                throw new Exception("No enabled scenes in Editor Build Settings.");
            }

            return scenes.ToArray();
        }

        /// <summary>
        /// Sets the SDK the iOS player's Xcode project builds against, <c>Device</c> or <c>Simulator</c>, and returns
        /// the one it replaced.
        /// </summary>
        public static string SetIosSdk(string sdk)
        {
            var previous = PlayerSettings.iOS.sdkVersion.ToString().Replace("SDK", "");
            iOSSdkVersion version;
            if (!Enum.TryParse(sdk + "SDK", true, out version))
            {
                throw new ArgumentException($"Unsupported iOS SDK '{sdk}': use Device or Simulator.");
            }

            PlayerSettings.iOS.sdkVersion = version;
            Debug.Log($"iOS SDK: {version}");
            return previous;
        }

        private static string GetArgument(string[] arguments, string name)
        {
            var value = FindArgument(arguments, name);
            if (value == null)
            {
                throw new ArgumentException($"Required command line argument '{name}' was not provided.");
            }

            return value;
        }

        private static string FindArgument(string[] arguments, string name)
        {
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }

        private static BuildTarget GetBuildTarget(string target)
        {
            BuildTarget buildTarget;
            if (Enum.TryParse(target, true, out buildTarget))
            {
                return buildTarget;
            }

            throw new ArgumentException($"Unsupported build target '{target}'.");
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                case BuildTarget.WebGL:
                    return BuildTargetGroup.WebGL;
                default:
                    throw new ArgumentException($"Unsupported build target '{target}'.");
            }
        }
    }
}
