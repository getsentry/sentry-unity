using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
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
        if (report.summary.platform == BuildTarget.StandaloneOSX)
        {
            // ATS applies to macOS players too and blocks plain HTTP to an IP literal, which is what
            // the envelope capture server is. The iOS module isn't available on macOS build agents,
            // so patch the plist as plain XML instead of going through PlistDocument.
            AllowArbitraryLoadsInMacPlist(Path.Combine(pathToBuiltProject, "Contents", "Info.plist"));
        }

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

    private static void AllowArbitraryLoadsInMacPlist(string plistPath)
    {
        if (!File.Exists(plistPath))
        {
            Debug.LogError($"Failed to find the plist at {plistPath}.");
            return;
        }

        var document = new XmlDocument { XmlResolver = null };
        // Parse (not Ignore) keeps the DOCTYPE in the document; the null resolver keeps us from
        // fetching the external DTD Apple references.
        using (var reader = XmlReader.Create(plistPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null }))
        {
            document.Load(reader);
        }

        var root = document.SelectSingleNode("/plist/dict");
        if (root is null)
        {
            Debug.LogError("Failed to find the root <dict> in the plist.");
            return;
        }

        foreach (XmlNode child in root.ChildNodes)
        {
            if (child.Name == "key" && child.InnerText == "NSAppTransportSecurity")
            {
                Debug.Log("AllowInsecureHttp: plist already contains NSAppTransportSecurity, nothing to do.");
                return;
            }
        }

        var key = document.CreateElement("key");
        key.InnerText = "NSAppTransportSecurity";
        var value = document.CreateElement("dict");
        var allowKey = document.CreateElement("key");
        allowKey.InnerText = "NSAllowsArbitraryLoads";
        value.AppendChild(allowKey);
        value.AppendChild(document.CreateElement("true"));

        root.AppendChild(key);
        root.AppendChild(value);
        document.Save(plistPath);

        // XmlDocument serializes the DOCTYPE with an empty internal subset (`...PropertyList-1.0.dtd"[]>`)
        // which Apple's plist parser rejects. Drop it again.
        var patched = Regex.Replace(File.ReadAllText(plistPath), @"(<!DOCTYPE[^>\[]*)\[\]>", "$1>");
        File.WriteAllText(plistPath, patched);

        Debug.Log("AllowInsecureHttp: added NSAllowsArbitraryLoads to the macOS plist.");
    }
}
