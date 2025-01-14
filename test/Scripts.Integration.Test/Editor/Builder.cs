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
    public static void BuildIl2CPPPlayer(BuildTarget target, BuildTargetGroup group, BuildOptions buildOptions)
    {
        Debug.Log("Builder: Starting to build");

        var args = ParseCommandLineArguments();
        ValidateArguments(args);

        Debug.Log($"Builder: Starting build. Output will be '{args["buildPath"]}'.");

        // Make sure the configuration is right.
        EditorUserBuildSettings.selectedBuildTargetGroup = group;
        EditorUserBuildSettings.allowDebugging = false;
        PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP);

        DisableUnityAudio();
        DisableProgressiveLightMapper();

        // This should make IL2CCPP builds faster, see https://forum.unity.com/threads/il2cpp-build-time-improvements-seeking-feedback.1064135/
        Debug.Log("Builder: Setting IL2CPP generation to OptimizeSize");
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.FromBuildTargetGroup(group), UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
#elif UNITY_2021_2_OR_NEWER
        EditorUserBuildSettings.il2CppCodeGeneration = UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize;
#endif

        // This is a workaround for build issues with Unity 2022.3. and newer.
        // https://discussions.unity.com/t/gradle-build-issues-for-android-api-sdk-35-in-unity-2022-3lts/1502187/10
#if UNITY_2022_3_OR_NEWER
        Debug.Log("Builder: Setting Android target API level to 33");
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
#endif

        Debug.Log("Builder: Updating BuildPlayerOptions");
        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = args["buildPath"],
            target = target,
            targetGroup = group,
            options = buildOptions
        };

        Debug.Log("Builder: Disabling optimizations to reduce build time");
#if UNITY_2021_2_OR_NEWER
        // TODO Linux fails with `free(): invalid pointer` in the smoke-test, after everthing seems to have shut down.
        if (target != BuildTarget.StandaloneLinux64)
        {
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
            // Android does not support appending builds. We make sure the directory is clean
            var outputDir = Path.GetDirectoryName(args["buildPath"]);
            if (Directory.Exists(outputDir))
            {
                Debug.Log("Builder: Cleaning the buildPath");
                Directory.Delete(outputDir, true);
            }

            Debug.Log($"Builder: Creating output directory at '{outputDir}'");
            Directory.CreateDirectory(outputDir);

            Debug.Log("Builder: Enabling minify");
#if UNITY_2020_1_OR_NEWER
            PlayerSettings.Android.minifyDebug = PlayerSettings.Android.minifyRelease = true;
#else
            EditorUserBuildSettings.androidDebugMinification =
                EditorUserBuildSettings.androidReleaseMinification = AndroidMinification.Proguard;
#endif

#if UNITY_6000_0_OR_NEWER
            Debug.Log("Builder: Setting target architectures");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All;
#endif
        }

        Debug.Log("Builder: Checking for SmokeTest scene");
        if (File.Exists("Assets/Scenes/SmokeTest.unity"))
        {
            Debug.Log("Builder: Adding SmokeTest.unity to scenes");
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/SmokeTest.unity" };
        }

        Debug.Log("Builder: Starting build");
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        Debug.Log("Builder: Build result at outputPath: " + summary.outputPath);

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log($"Builder: Build succeeded: {summary.totalSize} bytes");
                break;
            default:
                Debug.Log($"Builder: Build result: {summary.result} with {summary.totalErrors}" + $" error{(summary.totalErrors > 1 ? "s" : "")}.");
                throw new Exception("Build failed, see details above.");
        }

        if (summary.totalWarnings > 0)
        {
            Debug.Log($"Builder: Build succeeded with {summary.totalWarnings} warning{(summary.totalWarnings > 1 ? "s" : "")}.");
        }
    }

    public static void BuildWindowsIl2CPPPlayer()
    {
        Debug.Log("Builder: Building Windows IL2CPP Player");
        BuildIl2CPPPlayer(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, BuildOptions.StrictMode);
    }
    public static void BuildMacIl2CPPPlayer()
    {
        Debug.Log("Builder: Building macOS IL2CPP Player");
        BuildIl2CPPPlayer(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, BuildOptions.StrictMode);
    }
    public static void BuildLinuxIl2CPPPlayer()
    {
        Debug.Log("Builder: Building Linux IL2CPP Player");
        BuildIl2CPPPlayer(BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone, BuildOptions.StrictMode);
    }
    public static void BuildAndroidIl2CPPPlayer()
    {
        Debug.Log("Builder: Building Android IL2CPP Player");
        BuildIl2CPPPlayer(BuildTarget.Android, BuildTargetGroup.Android, BuildOptions.StrictMode);
    }
    public static void BuildAndroidIl2CPPProject()
    {
        Debug.Log("Builder: Building Android IL2CPP Project");
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        BuildIl2CPPPlayer(BuildTarget.Android, BuildTargetGroup.Android, BuildOptions.AcceptExternalModificationsToPlayer);
    }
    public static void BuildIOSProject()
    {
        Debug.Log("Builder: Building iOS Project");
        BuildIl2CPPPlayer(BuildTarget.iOS, BuildTargetGroup.iOS, BuildOptions.StrictMode);
    }
    public static void BuildWebGLPlayer()
    {
        Debug.Log("Builder: Building WebGL Player");
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        BuildIl2CPPPlayer(BuildTarget.WebGL, BuildTargetGroup.WebGL, BuildOptions.StrictMode);
    }

    public static Dictionary<string, string> ParseCommandLineArguments()
    {
        Debug.Log("Builder: Parsing command line arguments");
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
        Debug.Log("Builder: Validating command line arguments");
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

    // The Progressive Lightmapper does not work on silicone CPUs and there is no GPU in CI
    private static void DisableProgressiveLightMapper()
    {
#if UNITY_2021_OR_NEWER
        Lightmapping.lightingSettings = new LightingSettings
        {
            bakedGI = false
        };
#endif
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
