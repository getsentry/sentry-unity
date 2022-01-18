using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Builder
{
    public static void BuildIl2CPPPlayer(BuildTarget buildTarget)
    {
        var args = ParseCommandLineArguments();
        ValidateArguments(args);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] {"Assets/Scenes/1_Bugfarm.unity"},
            locationPathName = args["buildPath"],
            target = buildTarget,
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

    public static void BuildWindowsIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.StandaloneWindows64);
    public static void BuildMacIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.StandaloneOSX);
    public static void BuildAndroidIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.Android);
    public static void BuildIOSPlayer() => BuildIl2CPPPlayer(BuildTarget.iOS);

    private static Dictionary<string, string> ParseCommandLineArguments()
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
