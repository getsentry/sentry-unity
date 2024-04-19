using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
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
        DisableUnityAudio();
        EditorUserBuildSettings.allowDebugging = false;

        // This should make IL2CCPP builds faster, see https://forum.unity.com/threads/il2cpp-build-time-improvements-seeking-feedback.1064135/
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.FromBuildTargetGroup(group), UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
#elif UNITY_2021_2_OR_NEWER
        EditorUserBuildSettings.il2CppCodeGeneration = UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize;
#endif

        // Disable optimizations to reduce the build time.
#if UNITY_2021_2_OR_NEWER
        // TODO Linux fails with `free(): invalid pointer` in the smoke-test, after everthing seems to have shut down.
        if (target != BuildTarget.StandaloneLinux64) {
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.FromBuildTargetGroup(group), Il2CppCompilerConfiguration.Debug);
        }
#else
        // TODO Windows fails to build
        if (target != BuildTarget.StandaloneWindows64)
        {
            PlayerSettings.SetIl2CppCompilerConfiguration(group, Il2CppCompilerConfiguration.Debug);
        }
#endif
        if (target == BuildTarget.Android)
        {
#if UNITY_2020_1_OR_NEWER
            PlayerSettings.Android.minifyDebug = PlayerSettings.Android.minifyRelease = true;
            PlayerSettings.Android.minifyWithR8 = true;
#else
            EditorUserBuildSettings.androidDebugMinification =
                EditorUserBuildSettings.androidReleaseMinification = AndroidMinification.Proguard;
#endif
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = args["buildPath"],
            target = target,
            targetGroup = group,
            options = BuildOptions.StrictMode,
        };

        if (File.Exists("Assets/Scenes/SmokeTest.unity"))
        {
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/SmokeTest.unity" };
        }

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        Debug.Log("Build result at outputPath: " + summary.outputPath);

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                break;
            default:
                Debug.Log($"Build result: {summary.result} with {summary.totalErrors}" + $" error{(summary.totalErrors > 1 ? "s" : "")}.");
                throw new Exception("Build failed, see details above.");
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
    public static void BuildAndroidIl2CPPProject()
    {
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        BuildIl2CPPPlayer(BuildTarget.Android, BuildTargetGroup.Android);
    }
    public static void BuildIOSProject() => BuildIl2CPPPlayer(BuildTarget.iOS, BuildTargetGroup.iOS);
    public static void BuildWebGLPlayer()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        BuildIl2CPPPlayer(BuildTarget.WebGL, BuildTargetGroup.WebGL);
    }

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

public class AllowInsecureHttp : IPostprocessBuildWithReport, IPreprocessBuildWithReport
{
    public int callbackOrder { get; }
    public void OnPreprocessBuild(BuildReport report)
    {
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
#endif
    }

    // The `allow insecure http always` options don't seem to work. This is why we modify the info.plist directly.
    // Using reflection to get around the iOS module requirement on non-iOS platforms
    public void OnPostprocessBuild(BuildReport report)
    {
        var pathToBuiltProject = report.summary.outputPath;
        if (report.summary.platform == BuildTarget.iOS)
        {
            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogError("Failed to find the plist.");
                return;
            }

            var xcodeAssembly = Assembly.Load("UnityEditor.iOS.Extensions.Xcode");
            var plistType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.PlistDocument");
            var plistElementDictType = xcodeAssembly.GetType("UnityEditor.iOS.Xcode.PlistElementDict");

            var plist = Activator.CreateInstance(plistType);
            plistType.GetMethod("ReadFromString", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(plist, new object[] { File.ReadAllText(plistPath) });

            var root = plistType.GetField("root", BindingFlags.Public | BindingFlags.Instance);
            var allowDict = plistElementDictType.GetMethod("CreateDict", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(root?.GetValue(plist), new object[] { "NSAppTransportSecurity" });

            plistElementDictType.GetMethod("SetBoolean", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(allowDict, new object[] { "NSAllowsArbitraryLoads", true });

            var contents = (string)plistType.GetMethod("WriteToString", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(plist, null);

            File.WriteAllText(plistPath, contents);
        }
    }
}
