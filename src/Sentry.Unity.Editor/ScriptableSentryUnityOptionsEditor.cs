using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    [CustomEditor(typeof(ScriptableSentryUnityOptions))]
    public class ScriptableSentryUnityOptionsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target is not ScriptableSentryUnityOptions options)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.LabelField("Core", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Enable Sentry SDK", options.Enabled);
            EditorGUILayout.TextField("DSN", options.Dsn);
            EditorGUILayout.Toggle("Capture In Editor", options.CaptureInEditor);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.FloatField("Traces Sample Rate", (float)options.TracesSampleRate);
            EditorGUILayout.Toggle("Auto Startup Traces", options.AutoStartupTraces);
            EditorGUILayout.Toggle("Auto Scene Load Traces", options.AutoSceneLoadTraces);
            EditorGUILayout.Toggle("Attach Stacktrace", options.AutoAwakeTraces);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Enrichment", EditorStyles.boldLabel);
            EditorGUILayout.TextField("Release Override", options.ReleaseOverride);
            EditorGUILayout.TextField("Environment Override", options.EnvironmentOverride);
            EditorGUILayout.Toggle("Attach Stacktrace", options.AttachStacktrace);
            EditorGUILayout.Toggle("Attach Screenshot", options.AttachScreenshot);
            EditorGUILayout.Toggle("Attach Hierarchy", options.AttachViewHierarchy);
            EditorGUILayout.IntField("Max Breadcrumbs", options.MaxBreadcrumbs);
            EditorGUILayout.EnumPopup("Report Assemblies Mode", options.ReportAssembliesMode);
            EditorGUILayout.Toggle("Send Default Pii", options.SendDefaultPii);
            EditorGUILayout.Toggle("Auto Set UserName", options.IsEnvironmentUser);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Transport", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Enable Offline Caching", options.EnableOfflineCaching);
            EditorGUILayout.IntField("Max Cache Items", options.MaxCacheItems);
            EditorGUILayout.IntField("Init Flush Timeout [ms]", options.InitCacheFlushTimeout);
            EditorGUILayout.FloatField("Event Sample Rate", options.SampleRate);
            EditorGUILayout.IntField("Shut Down Timeout [ms]", options.ShutdownTimeout);
            EditorGUILayout.IntField("Max Queue Items", options.MaxQueueItems);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Auto Session Tracking", options.AutoSessionTracking);
            EditorGUILayout.IntField("Session Timeout [ms]", options.AutoSessionTrackingInterval);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Application Not Responding", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Enable ANR Detection", options.AnrDetectionEnabled);
            EditorGUILayout.IntField("ANR Timeout [ms]", options.AnrTimeout);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.Toggle("iOS Native Support", options.IosNativeSupportEnabled);
            EditorGUILayout.Toggle("Android Native Support", options.AndroidNativeSupportEnabled);
            EditorGUI.indentLevel++;
            EditorGUILayout.Toggle("NDK Integration", options.NdkIntegrationEnabled);
            EditorGUILayout.Toggle("NDK Scope Sync", options.NdkScopeSyncEnabled);
            EditorGUI.indentLevel--;
            EditorGUILayout.Toggle("Windows Native Support", options.WindowsNativeSupportEnabled);
            EditorGUILayout.Toggle("macOS Native Support", options.MacosNativeSupportEnabled);
            EditorGUILayout.Toggle("Linux Native Support", options.LinuxNativeSupportEnabled);
            EditorGUILayout.Toggle("IL2CPP line numbers", options.Il2CppLineNumberSupportEnabled);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Options Configuration", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Runtime Configuration", options.RuntimeOptionsConfiguration,
                typeof(SentryRuntimeOptionsConfiguration), false);
            EditorGUILayout.ObjectField("Build Time Configuration", options.BuildTimeOptionsConfiguration,
                typeof(SentryBuildTimeOptionsConfiguration), false);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("Enable Debug Output", options.Debug);
            EditorGUILayout.Toggle("Only In Editor", options.DebugOnlyInEditor);
            EditorGUILayout.EnumPopup("Verbosity level", options.DiagnosticLevel);

            EditorGUI.EndDisabledGroup();
        }
    }
}
