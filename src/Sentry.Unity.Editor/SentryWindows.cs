using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryWindows : EditorWindow
    {
        internal const string SentryOptionsAssetPath = "Assets/Resources/Sentry/SentryOptions.asset";

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
            GUILayout.Label("SDK Options", EditorStyles.boldLabel);
            Options.Enabled = EditorGUILayout.Toggle("Enabled", Options.Enabled);

            Options.Dsn = EditorGUILayout.TextField("DSN", Options.Dsn);

            Options.SampleRate = EditorGUILayout.Slider("Sample rate for errors", Options.SampleRate, 0, 1);

            Options.Debug = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Debug Mode", "Whether the Sentry SDK should print its diagnostic logs to the console."),
                Options.Debug);

            Options.DebugOnlyInEditor = EditorGUILayout.Toggle(
                "Only In Editor",
                Options.DebugOnlyInEditor);

            Options.DiagnosticsLevel = (SentryLevel)EditorGUILayout.EnumPopup("Verbosity level:", Options.DiagnosticsLevel);
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
