using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Tests
{
    public class ScriptableSentryUnityOptionsTests
    {
        [Test]
        public void ScriptableSentryUnityOptions_Creation_AllPropertiesPresent()
        {
            const string testOptionsPath = "Assets/TestOptions.asset";

            var scriptableOptions = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
            AssetDatabase.CreateAsset(scriptableOptions, testOptionsPath);
            AssetDatabase.SaveAssets();

            var optionsAsString = File.ReadAllText(testOptionsPath);

            StringAssert.Contains("Enabled", optionsAsString);
            StringAssert.Contains("Dsn", optionsAsString);
            StringAssert.Contains("CaptureInEditor", optionsAsString);
            StringAssert.Contains("EnableLogDebouncing", optionsAsString);
            StringAssert.Contains("TracesSampleRate", optionsAsString);
            StringAssert.Contains("AutoSessionTracking", optionsAsString);
            StringAssert.Contains("AutoSessionTrackingInterval", optionsAsString);
            StringAssert.Contains("ReleaseOverride", optionsAsString);
            StringAssert.Contains("EnvironmentOverride", optionsAsString);
            StringAssert.Contains("AttachStacktrace", optionsAsString);
            StringAssert.Contains("AttachScreenshot", optionsAsString);
            StringAssert.Contains("ScreenshotMaxWidth", optionsAsString);
            StringAssert.Contains("ScreenshotMaxHeight", optionsAsString);
            StringAssert.Contains("ScreenshotQuality", optionsAsString);
            StringAssert.Contains("MaxBreadcrumbs", optionsAsString);
            StringAssert.Contains("ReportAssembliesMode", optionsAsString);
            StringAssert.Contains("SendDefaultPii", optionsAsString);
            StringAssert.Contains("IsEnvironmentUser", optionsAsString);
            StringAssert.Contains("EnableOfflineCaching", optionsAsString);
            StringAssert.Contains("MaxCacheItems", optionsAsString);
            StringAssert.Contains("InitCacheFlushTimeout", optionsAsString);
            StringAssert.Contains("ShutdownTimeout", optionsAsString);
            StringAssert.Contains("MaxQueueItems", optionsAsString);
            StringAssert.Contains("IosNativeSupportEnabled", optionsAsString);
            StringAssert.Contains("AndroidNativeSupportEnabled", optionsAsString);
            StringAssert.Contains("WindowsNativeSupportEnabled", optionsAsString);
            StringAssert.Contains("MacosNativeSupportEnabled", optionsAsString);
            StringAssert.Contains("OptionsConfiguration", optionsAsString);
            StringAssert.Contains("Debug", optionsAsString);
            StringAssert.Contains("DebugOnlyInEditor", optionsAsString);
            StringAssert.Contains("DiagnosticLevel", optionsAsString);

            AssetDatabase.DeleteAsset(testOptionsPath);
            AssetDatabase.Refresh();
        }
    }
}
