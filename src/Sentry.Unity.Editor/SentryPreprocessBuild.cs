using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; private set; }

        public void OnPreprocessBuild(BuildReport report)
        {
            switch (report.summary.platform)
            {
                case BuildTarget.iOS:
                    CheckIOSEditorImportSettings(Path.Combine("Packages", SentryPackageInfo.GetName(), "Editor", "iOS",
                        "Sentry.Unity.Editor.iOS", ".meta"));
                    break;
                default:
                    // nothing to do
                    break;
            }
        }

        internal void CheckIOSEditorImportSettings(string metaFilePath, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;
            if (application.Platform == RuntimePlatform.OSXEditor)
            {
                return;
            }

            if (!File.Exists(metaFilePath))
            {
                return;
            }

            var meta = File.ReadAllText(metaFilePath);
            if (meta.Contains("OS: OSX"))
            {
                new UnityLogger(new SentryUnityOptions()).LogWarning("You're trying to build for iOS on " +
                    "Windows but the import settings for 'Sentry.Unity.Editor.iOS' are set to OSX only. Sentry will " +
                    "not be included in your Xcode project. To fix this you have to embed the Sentry package and set " +
                    "the platform settings for 'Sentry/Editor/iOS/Sentry.Unity.Editor.iOS.dll' to 'Any OS'.");
            }
        }
    }
}
