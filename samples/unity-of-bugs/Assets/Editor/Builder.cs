using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Builder
{
    public static void BuildIl2CPPPlayer(BuildTarget buildTarget)
    {
        var artifactPath = GetArg("-artifactPath");
        if (artifactPath is null)
        {
            throw new Exception("No valid '-artifactPath' has been provided.");
        }

        var artifactName = GetArg("-artifactName");
        if (artifactName is null)
        {
            throw new Exception("No valid '-artifactName' has been provided.");
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] {"Assets/Scenes/1_BugfarmScene.unity"},
            locationPathName = Path.Combine(artifactPath, artifactName),
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

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
