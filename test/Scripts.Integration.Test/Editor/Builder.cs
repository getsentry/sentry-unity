using System;
using System.Collections.Generic;
using System.IO;
using Sentry.Unity.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Builder
{
    public static void BuildIl2CPPPlayer(BuildTarget target, BuildTargetGroup group)
    {
        var args = ParseCommandLineArguments();
        ValidateArguments(args);

        // Make sure the configuration is right.
        EditorUserBuildSettings.selectedBuildTargetGroup = group;
        PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP);

        var cliOptions = AssetDatabase.LoadAssetAtPath<SentryCliOptions>(Path.Combine("Assets", "Plugins", "Sentry", "SentryCliOptions.asset"));

        // 'build-project.ps1' explicitely calls for uploading symbols
        if(args.ContainsKey("uploadSymbols"))
        {
            Debug.Log("Enabling automated debug symbol upload.");
            cliOptions.UploadSymbols = true;
        }
        else
        {
            Debug.Log("Disabling automated debug symbol upload.");
            cliOptions.UploadSymbols = false;
        }

        EditorUtility.SetDirty(cliOptions);
        AssetDatabase.SaveAssets();

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/1_Bugfarm.unity" },
            locationPathName = args["buildPath"],
            target = target,
            targetGroup = group,
            options = BuildOptions.StrictMode,
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        Debug.Log("Build result at outputPath: " + report.summary.outputPath);

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                break;
            default:
                var message = $"Build result: {summary.result} with {summary.totalErrors}" +
                              $" error{(summary.totalErrors > 1 ? "s" : "")}.";

                Debug.Log(message);
                throw new Exception(message);
        }

        if (summary.totalErrors > 0)
        {
            var message = $"Build succeeded with {summary.totalErrors} error{(summary.totalErrors > 1 ? "s" : "")}.";
            Debug.Log(message);
            // Break the build
            throw new Exception(message);
        }

        if (summary.totalWarnings > 0)
        {
            Debug.Log($"Build succeeded with {summary.totalWarnings} warning{(summary.totalWarnings > 1 ? "s" : "")}.");
        }
    }
    public static void BuildWindowsIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone);
    public static void BuildMacIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone);
    public static void BuildLinuxIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone);
    public static void BuildAndroidIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.Android, BuildTargetGroup.Android);
    public static void BuildIOSPlayer() => BuildIl2CPPPlayer(BuildTarget.iOS, BuildTargetGroup.iOS);
    public static void BuildWebGLPlayer() => BuildIl2CPPPlayer(BuildTarget.WebGL, BuildTargetGroup.WebGL);

    public static Dictionary<string, string> ParseCommandLineArguments()
    {
        var commandLineArguments = new Dictionary<string, string>();
        var args = Environment.GetCommandLineArgs();

        for (int current = 0, next = 1; current < args.Length; current++, next++)
        {
            if (!args[current].StartsWith("-"))
            {
                continue;
            }

            var flag = args[current].TrimStart('-');
            var flagHasValue = next < args.Length && !args[next].StartsWith("-");
            var flagValue = flagHasValue ? args[next].TrimStart('-') : "";

            commandLineArguments.Add(flag, flagValue);
        }

        return commandLineArguments;
    }

    private static void ValidateArguments(Dictionary<string, string> args)
    {
        if (!args.ContainsKey("buildPath") || string.IsNullOrWhiteSpace(args["buildPath"]))
        {
            throw new Exception("No valid '-buildPath' has been provided.");
        }
    }
}
