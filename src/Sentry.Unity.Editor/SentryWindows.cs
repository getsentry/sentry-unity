using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryWindows : EditorWindow
    {
        private const string SentryOptionsAssetPath = "Assets/Resources/Sentry/SentryOptions.asset";

        [MenuItem("Component/Sentry")]
        public static void OpenSentryWindow() => GetWindow(typeof(SentryWindows));

        public UnitySentryOptions Options { get; set; }

        protected void OnEnable()
        {
            var isDarkMode = EditorGUIUtility.isProSkin;
            const string outputDir = "Assets/Editor/Sentry.Unity/"; // To stay in sync with csproj <OutDir>
            var icon = EditorGUIUtility.Load($"{outputDir}SentryLogo{(isDarkMode?"Light":"Dark")}.png") as Texture2D;
            titleContent = new GUIContent("Sentry", icon, "Sentry SDK Options");

            Options = AssetDatabase.LoadAssetAtPath<UnitySentryOptions>(SentryOptionsAssetPath);
            if (Options is null)
            {
                Options = CreateInstance<UnitySentryOptions>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Sentry"))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Sentry");
                }
                AssetDatabase.CreateAsset(Options , SentryOptionsAssetPath);
            }

            EditorUtility.SetDirty(Options);
        }

        private void Validate()
        {
            if (!Options.Enabled)
            {
                return;
            }

            if (Options.Dsn == null)
            {
                Options.Dsn = null;
                // Debug.LogError("Missing Sentry DSN.");
            }
            else if (!Uri.IsWellFormedUriString(Options.Dsn, UriKind.Absolute))
            {
                Options.Dsn = null;
                Debug.LogError("Invalid DSN format. Expected a URL.");
            }
        }

        private void OnLostFocus()
        {
            Validate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            GUILayout.Label(new GUIContent(GUIContent.none), EditorStyles.boldLabel);
            Options.Enabled = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Enable", "Controls enabling Sentry by initializing the SDK or not."),
                Options.Enabled);
            Options.CaptureInEditor = EditorGUILayout.Toggle(
                new GUIContent("Capture In Editor", "Capture errors while running in the Editor."),
                Options.CaptureInEditor);

            GUILayout.Label(new GUIContent(GUIContent.none), EditorStyles.boldLabel);
            GUILayout.Label("SDK Options", EditorStyles.boldLabel);
            Options.Dsn = EditorGUILayout.TextField(
                new GUIContent("DSN", "The URL to your project inside Sentry. Get yours in Sentry, Project Settings."),
                Options.Dsn);
            Options.SampleRate = EditorGUILayout.Slider(
                new GUIContent("Event Sample Rate", "What random sample rate to apply. 1.0 captures everything, 0.7 captures 70%."),
                Options.SampleRate, 0.01f, 1);
            Options.RequestBodyCompressionLevel = (SentryUnityCompression)EditorGUILayout.EnumPopup(
                new GUIContent("Compress Payload", "The compression level to use on the data sent to Sentry. " +
                                                   "Some platforms don't support GZip, 'auto' attempts to disable compression in those cases."),
                Options.RequestBodyCompressionLevel);
            Options.AttachStacktrace = EditorGUILayout.Toggle(
                new GUIContent("Stacktrace For Logs", "Whether to include a stack trace for non error events like logs. " +
                                                                "Even when Unity didn't include and no Exception was thrown.."),
                Options.AttachStacktrace);
            Options.Release = EditorGUILayout.TextField(
                new GUIContent("Override Release", "By default release is taken from 'Application.version'. " +
                                                   "This option is an override."),
                Options.Release);
            Options.Environment = EditorGUILayout.TextField(
                new GUIContent("Override Environment", "An explicit environment. " +
                                                       "If not set, auto detects such as 'development', 'production' or 'editor'."),
                Options.Environment);

            GUILayout.Label(new GUIContent(GUIContent.none), EditorStyles.boldLabel);
            Options.Debug = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Debug Mode", "Whether the Sentry SDK should print its diagnostic logs to the console."),
                Options.Debug);
            Options.DebugOnlyInEditor = EditorGUILayout.Toggle(
                new GUIContent(
                    "Only In Editor",
                    "Only print logs when in the editor. Development builds of the player will not include Sentry's SDK diagnostics."),
                Options.DebugOnlyInEditor);
            Options.DiagnosticsLevel = (SentryLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Verbosity level", "The minimum level allowed to be printed to the console. " +
                                                  "Log messages with a level below this level are dropped."),
                Options.DiagnosticsLevel);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.EndToggleGroup();

            // groupEnabled = EditorGUILayout.BeginToggleGroup("Sentry CLI Options", groupEnabled);
            // uploadSymbols = EditorGUILayout.Toggle("Upload Proguard Mappings", uploadSymbols);
            // auth = EditorGUILayout.TextField("Auth token", auth);
            // organization = EditorGUILayout.TextField("Organization", organization);
            // project = EditorGUILayout.TextField("Project", project);
            // EditorGUILayout.EndToggleGroup();
        }
    }
}
