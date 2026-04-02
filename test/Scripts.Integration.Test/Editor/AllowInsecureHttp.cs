using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
