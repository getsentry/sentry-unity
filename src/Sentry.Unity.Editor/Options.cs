using System;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryWindows : EditorWindow
    {
        [MenuItem("Component/Sentry")]
        public static void OpenSentryWindow() => GetWindow(typeof(SentryWindows));

        public SentryOptionsScriptableObject Options { get; set; }

        protected void OnEnable()
        {
            var isDarkMode = EditorGUIUtility.isProSkin;
            const string outputDir = "Assets/Editor/Sentry.Unity/"; // To stay in sync with csproj <OutDir>
            var icon = EditorGUIUtility.Load($"{outputDir}SentryLogo{(isDarkMode?"Light":"Dark")}.png") as Texture2D;
            titleContent = new GUIContent("Sentry", icon, "Sentry SDK Options");

            const string sentryOptionsAssetPath = "Assets/Resources/Sentry/SentryOptions.asset";
            Options = AssetDatabase.LoadAssetAtPath<SentryOptionsScriptableObject>(sentryOptionsAssetPath);
            // Options = Resources.Load(sentryOptionsAssetPath) as SentryOptionsScriptableObject;
            if (Options is null)
            {
                Options = CreateInstance<SentryOptionsScriptableObject>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Sentry"))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Sentry");
                }
                AssetDatabase.CreateAsset(Options , sentryOptionsAssetPath);
            }
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
                Debug.LogError("Missing Sentry DSN.");
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
        }

        private void OnGUI()
        {
            Options.Enabled = EditorGUILayout.Toggle("Enabled", Options.Enabled);
            GUILayout.Label("SDK Options", EditorStyles.boldLabel);
            Options.Dsn = EditorGUILayout.TextField("DSN", Options.Dsn);

            // sample = EditorGUILayout.Slider("Sample rate for errors", sample, 0, 1);
            //
            // groupEnabled = EditorGUILayout.BeginToggleGroup("Sentry CLI Options", groupEnabled);
            // uploadSymbols = EditorGUILayout.Toggle("Upload Proguard Mappings", uploadSymbols);
            // auth = EditorGUILayout.TextField("Auth token", auth);
            // organization = EditorGUILayout.TextField("Organization", organization);
            // project = EditorGUILayout.TextField("Project", project);
            // EditorGUILayout.EndToggleGroup();
        }
    }

    public class SentryOptionsScriptableObject : ScriptableObject
    {
        public bool Enabled { get; set; } = true;
        public string Dsn { get; set; }
        // private bool groupEnabled;
        // [SerializeField]
        // private bool debug = true;
        // [SerializeField]
        // private float sample = 1.0f;
        //
        // [SerializeField]
        // private bool uploadSymbols = true;
        // [SerializeField]
        // private string auth;
        // [SerializeField]
        // private string organization;
        // [SerializeField]
        // private string project;
    }
}
