using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryPreprocessBuild : IPreprocessBuildWithReport
    {
        public static string LinkXmlFolder => "io.sentry";

        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            // WriteLinkXmlToAssets();

            // var options = AssetDatabase.LoadAssetAtPath<SentryCliOptions>(SentryWindows.SentryCliOptionsAssetPath);

            switch (report.summary.platform)
            {
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.tvOS:
                case BuildTarget.iOS:
                case BuildTarget.XboxOne: // pdbs?
                    // platformGroup = Standalone
                    // sentry-cli upload-dif
                    break;
                case BuildTarget.WebGL:
                    // sentry-cli sourcemaps
                    break;
                case BuildTarget.Android:
                    // sentry-cli proguard
                    break;
                default:
                    // nothing to do
                break;
            }
            // file: summary.outputPath
            // guid: summary.guid
        }

        internal void WriteLinkXmlToAssets()
        {
            var linkXmlPath = $"Assets/{LinkXmlFolder}/link.xml";

            AssetDatabase.CreateFolder("Assets", LinkXmlFolder);
            using var fileStream = File.Create(linkXmlPath);

            using var resourceStream = GetType().Assembly.GetManifestResourceStream("Sentry.Unity.Editor.link.xml");
            resourceStream.CopyTo(fileStream);

            fileStream.Close();
            resourceStream.Flush();

            AssetDatabase.ImportAsset(linkXmlPath);
        }
    }
}
