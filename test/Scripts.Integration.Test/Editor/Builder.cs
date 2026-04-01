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

        Debug.Log("Builder: Parsing command line arguments");
        var args = CommandLineArguments.Parse();
        ValidateArguments(args);

        Debug.Log($"Builder: Starting build. Output will be '{args["buildPath"]}'.");

        // Make sure the configuration is right.
        EditorUserBuildSettings.selectedBuildTargetGroup = group;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(group), ScriptingImplementation.IL2CPP);
        // Making sure that the app keeps on running in the background. Linux CI is very unhappy with coroutines otherwise.
        PlayerSettings.runInBackground = true;

        DisableUnityAudio();
        DisableProgressiveLightMapper();

        Debug.Log("Builder: Setting IL2CPP generation to OptimizeSpeed");
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.FromBuildTargetGroup(group), UnityEditor.Build.Il2CppCodeGeneration.OptimizeSpeed);
#elif UNITY_2021_2_OR_NEWER
        EditorUserBuildSettings.il2CppCodeGeneration = UnityEditor.Build.Il2CppCodeGeneration.OptimizeSpeed;
#endif

        Debug.Log("Builder: Configuring code stripping level");
#if UNITY_6000_0_OR_NEWER
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(group), ManagedStrippingLevel.High);
#else
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.FromBuildTargetGroup(group), ManagedStrippingLevel.Low);
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
        // TODO Linux fails with `free(): invalid pointer` in the test, after everything seems to have shut down.
        if (target != BuildTarget.StandaloneLinux64)
        {
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.FromBuildTargetGroup(group), Il2CppCompilerConfiguration.Debug);
        }

        if (target == BuildTarget.Android)
        {
            Debug.Log("Builder: Setting application identifier");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "io.sentry.unity.integrationtest");

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
            PlayerSettings.Android.minifyDebug = PlayerSettings.Android.minifyRelease = true;

#if UNITY_6000_0_OR_NEWER
            Debug.Log("Builder: Setting target architectures");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All;
#endif
        }

        Debug.Log("Builder: Checking for Test scene");
        if (File.Exists("Assets/Scenes/Test.unity"))
        {
            Debug.Log("Builder: Adding Test.unity to scenes");
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/Test.unity" };
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

    [MenuItem("Tools/Builder/Windows")]
    public static void BuildWindowsIl2CPPPlayer()
    {
        Debug.Log("Builder: Building Windows IL2CPP Player");
        BuildIl2CPPPlayer(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/macOS")]
    public static void BuildMacIl2CPPPlayer()
    {
        Debug.Log("Builder: Building macOS IL2CPP Player");
        BuildIl2CPPPlayer(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/Linux")]
    public static void BuildLinuxIl2CPPPlayer()
    {
        Debug.Log("Builder: Building Linux IL2CPP Player");
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore });
        PlayerSettings.gpuSkinning = false;
        PlayerSettings.graphicsJobs = false;
        BuildIl2CPPPlayer(BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/Android")]
    public static void BuildAndroidIl2CPPPlayer()
    {
        Debug.Log("Builder: Building Android IL2CPP Player");

        // Force OpenGLES3 to avoid Vulkan issues with the Android emulator in CI.
        // The emulator's swiftshader Vulkan implementation doesn't fully support Unity's
        // Vulkan usage, causing "Processed some Vulkan packets without process resources
        // created" warnings and SIGSEGV crashes in libvulkan_enc.so.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

#if UNITY_2021_2_OR_NEWER && !UNITY_6000_0_OR_NEWER
        // Clean Android gradle cache to force regeneration of gradle files
        // This prevents Unity from reusing gradle files that may contain Sentry symbol upload tasks from previous builds
        var androidGradlePath = Path.Combine(Directory.GetCurrentDirectory(), "Library/Bee/Android");
        if (Directory.Exists(androidGradlePath))
        {
            Debug.Log($"Builder: Cleaning Android gradle cache at '{androidGradlePath}'");
            Directory.Delete(androidGradlePath, true);
        }
#endif

        BuildIl2CPPPlayer(BuildTarget.Android, BuildTargetGroup.Android, BuildOptions.StrictMode);
    }
    [MenuItem("Tools/Builder/Android Project")]
    public static void BuildAndroidIl2CPPProject()
    {
        Debug.Log("Builder: Building Android IL2CPP Project");
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        BuildIl2CPPPlayer(BuildTarget.Android, BuildTargetGroup.Android, BuildOptions.AcceptExternalModificationsToPlayer);
    }

    [MenuItem("Tools/Builder/iOS")]
    public static void BuildIOSProject()
    {
        Debug.Log("Builder: Building iOS Project");
        BuildIl2CPPPlayer(BuildTarget.iOS, BuildTargetGroup.iOS, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/WebGL")]
    public static void BuildWebGLPlayer()
    {
        Debug.Log("Builder: Building WebGL Player");
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        BuildIl2CPPPlayer(BuildTarget.WebGL, BuildTargetGroup.WebGL, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/Switch")]
    public static void BuildSwitchIL2CPPPlayer()
    {
        Debug.Log("Builder: Building Switch IL2CPP Player");
        SetSwitchCreateNspRomFile();
        BuildIl2CPPPlayer(BuildTarget.Switch, BuildTargetGroup.Switch, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/Xbox Series X|S")]
    public static void BuildXSXIL2CPPPlayer()
    {
        Debug.Log("Builder: Building Xbox Series X|S IL2CPP Player");
        SetXboxSubtargetToMaster();
        BuildIl2CPPPlayer(BuildTarget.GameCoreXboxSeries, BuildTargetGroup.GameCoreXboxSeries, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/Xbox One")]
    public static void BuildXB1IL2CPPPlayer()
    {
        Debug.Log("Builder: Building Xbox One IL2CPP Player");
        SetXboxSubtargetToMaster();
        BuildIl2CPPPlayer(BuildTarget.GameCoreXboxOne, BuildTargetGroup.GameCoreXboxOne, BuildOptions.StrictMode);
    }

    [MenuItem("Tools/Builder/PS5")]
    public static void BuildPS5IL2CPPPlayer()
    {
        Debug.Log("Builder: Building PS5 IL2CPP Player");
        SetPS5BuildTypeToPackage();
        BuildIl2CPPPlayer(BuildTarget.PS5, BuildTargetGroup.PS5, BuildOptions.StrictMode);
    }

    private static void SetXboxSubtargetToMaster()
    {
        // The actual editor API to set this has been deprecated: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/XboxBuildSubtarget.html
        // Modifying the build profiles and build setting assets on disk does not work. Some of the properties are
        // stored inside a binary. Instead we're setting the properties via reflection and then saving the asset.
        var buildProfileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.CoreModule");
        if (buildProfileType == null)
        {
            return;
        }

        foreach (var profile in Resources.FindObjectsOfTypeAll(buildProfileType))
        {
            // BuildTarget.GameCoreXboxSeries = 42, BuildTarget.GameCoreXboxOne = 43.
            var buildTarget = new SerializedObject(profile).FindProperty("m_BuildTarget")?.intValue ?? -1;
            if (buildTarget != 42 && buildTarget != 43)
                continue;

            var platformSettings = buildProfileType
                .GetProperty("platformBuildProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(profile);
            var settingsData = platformSettings?.GetType()
                .GetField("m_settingsData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(platformSettings);

            GetFieldInHierarchy(settingsData?.GetType(), "buildSubtarget")?.SetValue(settingsData, 1); // 1 = Master
            GetFieldInHierarchy(platformSettings?.GetType(), "m_Development")?.SetValue(platformSettings, false);
            GetFieldInHierarchy(settingsData?.GetType(), "deploymentMethod")?.SetValue(settingsData, 2); // 2 = Package

            EditorUtility.SetDirty(profile);
            Debug.Log($"Builder: Xbox Build Profile (BuildTarget {buildTarget}) set to Master, deploy method set to Package");
        }

        AssetDatabase.SaveAssets();
    }

    private static void SetPS5BuildTypeToPackage()
    {
        var buildProfileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.CoreModule");
        if (buildProfileType == null)
        {
            return;
        }

        foreach (var profile in Resources.FindObjectsOfTypeAll(buildProfileType))
        {
            // BuildTarget.PS5 = 44.
            var buildTarget = new SerializedObject(profile).FindProperty("m_BuildTarget")?.intValue ?? -1;
            if (buildTarget != 44)
                continue;

            var platformSettings = buildProfileType
                .GetProperty("platformBuildProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(profile);

            GetFieldInHierarchy(platformSettings?.GetType(), "m_Development")?.SetValue(platformSettings, false);
            GetFieldInHierarchy(platformSettings?.GetType(), "m_BuildSubtarget")?.SetValue(platformSettings, 1); // 1 = Package

            EditorUtility.SetDirty(profile);
            Debug.Log("Builder: PS5 Build Profile set to Package");
        }

        AssetDatabase.SaveAssets();
    }

    private static void SetSwitchCreateNspRomFile()
    {
        var buildProfileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.CoreModule");
        if (buildProfileType == null)
        {
            return;
        }

        foreach (var profile in Resources.FindObjectsOfTypeAll(buildProfileType))
        {
            // BuildTarget.Switch = 38.
            var buildTarget = new SerializedObject(profile).FindProperty("m_BuildTarget")?.intValue ?? -1;
            if (buildTarget != 38)
                continue;

            var platformSettings = buildProfileType
                .GetProperty("platformBuildProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(profile);

            GetFieldInHierarchy(platformSettings?.GetType(), "m_Development")?.SetValue(platformSettings, false);
            GetFieldInHierarchy(platformSettings?.GetType(), "m_SwitchCreateRomFile")?.SetValue(platformSettings, 1); // 1 = enabled

            EditorUtility.SetDirty(profile);
            Debug.Log("Builder: Switch Build Profile set to Create NSP ROM File");
        }

        AssetDatabase.SaveAssets();
    }

    private static FieldInfo GetFieldInHierarchy(Type type, string fieldName)
    {
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field != null)
                return field;
            type = type.BaseType;
        }
        return null;
    }

    private static void ValidateArguments(Dictionary<string, string> args)
    {
        Debug.Log("Builder: Validating command line arguments");
        if (!args.ContainsKey("buildPath") || string.IsNullOrWhiteSpace(args["buildPath"]))
        {
            args["buildPath"] = "./Builds/";
            Debug.Log("Builder: No '-buildPath' provided, defaulting to './Builds/'");
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
