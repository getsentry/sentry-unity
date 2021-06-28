using UnityEditor;

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

            EditorGUILayout.Toggle("Enabled", options.Enabled);
            EditorGUILayout.TextField("DSN", options.Dsn);
            EditorGUILayout.Toggle("Capture In Editor", options.CaptureInEditor);
            EditorGUILayout.FloatField("Trace Sample Rate", (float)options.TracesSampleRate);
            EditorGUILayout.Toggle("Auto Session Tracking", options.AutoSessionTracking);
            EditorGUILayout.IntField("Session Timeout [ms]", options.AutoSessionTrackingInterval);

            EditorGUILayout.TextField("Release Override", options.ReleaseOverride);
            EditorGUILayout.TextField("Environment Override", options.EnvironmentOverride);
            EditorGUILayout.Toggle("Attach Stacktrace", options.AttachStacktrace);
            EditorGUILayout.IntField("Max Breadcrumbs", options.MaxBreadcrumbs);
            EditorGUILayout.EnumPopup("Report Assemblies Mode", options.ReportAssembliesMode);
            EditorGUILayout.Toggle("Send Default Pii", options.SendDefaultPii);
            EditorGUILayout.Toggle("Auto Set UserName", options.IsEnvironmentUser);
            EditorGUILayout.TextField("Server Name Override", options.ServerNameOverride);

            EditorGUILayout.Toggle("Enable Offline Caching", options.EnableOfflineCaching);
            EditorGUILayout.IntField("Max Cache Items", options.MaxCacheItems);
            EditorGUILayout.IntField("Init Flush Timeout [ms]", options.InitCacheFlushTimeout);
            EditorGUILayout.FloatField("Event Sample Rate", options.SampleRate ?? 1.0f);
            EditorGUILayout.IntField("Shut Down Timeout [ms]", options.ShutdownTimeout);
            EditorGUILayout.IntField("Max Queue Items", options.MaxQueueItems);

            EditorGUILayout.Toggle("Debug", options.Debug);
            EditorGUILayout.Toggle("Only In Editor", options.DebugOnlyInEditor);
            EditorGUILayout.EnumPopup("Verbosity level", options.DiagnosticLevel);

            EditorGUI.EndDisabledGroup();
        }
    }
}
