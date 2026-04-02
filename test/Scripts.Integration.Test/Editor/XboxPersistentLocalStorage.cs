using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Ensures Xbox builds have PersistentLocalStorage configured in the project's game config.
/// Required for sentry-native to write its crash database and for integration test logging.
/// </summary>
public class XboxPersistentLocalStorage : IPreprocessBuildWithReport
{
    public int callbackOrder { get; }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.GameCoreXboxSeries
            && report.summary.platform != BuildTarget.GameCoreXboxOne)
        {
            return;
        }

        var configName = report.summary.platform == BuildTarget.GameCoreXboxSeries
            ? "ScarlettGame.config"
            : "XboxOneGame.config";
        var configPath = Path.Combine("ProjectSettings", configName);

        var doc = new XmlDocument();
        doc.Load(configPath);

        var game = doc.DocumentElement;
        var pls = game["PersistentLocalStorage"] ?? doc.CreateElement("PersistentLocalStorage");
        if (pls.ParentNode == null)
        {
            game.AppendChild(pls);
        }

        pls.InnerXml = "<SizeMB>11</SizeMB><GrowableToMB>22</GrowableToMB>";

        doc.Save(configPath);
        Debug.Log($"XboxPersistentLocalStorage: Configured PersistentLocalStorage in {configName}");
    }
}
