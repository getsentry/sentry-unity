using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
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
        DisableUnityAudio();

        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = args["buildPath"],
            target = target,
            targetGroup = group,
            options = BuildOptions.StrictMode,
        };

        if(File.Exists("Assets/Scenes/SmokeTest.unity"))
        {
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/SmokeTest.unity" };
        }

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

    // Audio created issues, especially for iOS simulator so we disable it.
    private static void DisableUnityAudio()
    {
        var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0];
        var serializedManager = new SerializedObject(audioManager);
        var prop = serializedManager.FindProperty("m_DisableAudio");
        prop.boolValue = true;
        serializedManager.ApplyModifiedProperties();
    }
}

// This started being necessary with Unity 2022 and older iOS versions (12.0 - 14.1)
// Message: App Transport Security has blocked a cleartext HTTP (http://) resource load since it is insecure. Temporary exceptions can be configured via your app's Info.plist file.
public class iOSCleartextHTTP : IPostprocessBuildWithReport
{
    public int callbackOrder { get; }

    public void OnPostprocessBuild(BuildReport report)
    {
        var pathToBuiltProject = report.summary.outputPath;
        if (report.summary.platform == BuildTarget.iOS)
        {
            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            var allowsDict = plist.root.CreateDict("NSAppTransportSecurity");
            allowsDict.SetBoolean("NSAllowsArbitraryLoads", true);

            var exceptionsDict = allowsDict.CreateDict("NSExceptionDomains");

            var domainDict = exceptionsDict.CreateDict("localhost.exception");
            domainDict.SetBoolean("NSExceptionAllowsInsecureHTTPLoads", true);
            domainDict.SetBoolean("NSIncludesSubdomains", true);

            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
