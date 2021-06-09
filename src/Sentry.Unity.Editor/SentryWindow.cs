using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity.Editor
{
    public class SentryWindow : EditorWindow
    {
        [MenuItem("Tools/Sentry")]
        public static SentryWindow OpenSentryWindow()
            => (SentryWindow)GetWindow(typeof(SentryWindow));

        protected virtual string SentryOptionsAssetName { get; } = ScriptableSentryUnityOptions.ConfigName;

        public ScriptableSentryUnityOptions Options { get; private set; } = null!; // Set by OnEnable()

        public event Action<ValidationError> OnValidationError = _ => { };

        private void OnEnable()
        {
            SetTitle();
            Options = LoadOptions();
        }

        private ScriptableSentryUnityOptions LoadOptions()
        {
            var options = AssetDatabase.LoadAssetAtPath(
                ScriptableSentryUnityOptions.GetConfigPath(), typeof(ScriptableSentryUnityOptions)) as ScriptableSentryUnityOptions;

            if (options == null)
            {
                CreateConfigDirectory();
                options = CreateInstance<ScriptableSentryUnityOptions>();
                SentryOptionsUtility.SetDefaults(options);

                AssetDatabase.CreateAsset(options, ScriptableSentryUnityOptions.GetConfigPath());

                EditorUtility.SetDirty(options);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return options;
        }

        // ReSharper disable once UnusedMember.Local
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

            Options.RequestBodyCompressionLevel = (CompressionLevelWithAuto)EditorGUILayout.EnumPopup(
                new GUIContent("Compress Payload", "The level of which to compress the Sentry event before sending to Sentry."),
                Options.RequestBodyCompressionLevel);

            Options.AttachStacktrace = EditorGUILayout.Toggle(
                new GUIContent("Stacktrace For Logs", "Whether to include a stack trace for non error events like logs. " +
                                                      "Even when Unity didn't include and no Exception was thrown.."),
                Options.AttachStacktrace);

            Options.ReleaseOverride = EditorGUILayout.TextField(
                new GUIContent("Override Release", "By default release is taken from " +
                                                   "'Application.version'. This option is an override."),
                Options.ReleaseOverride);

            Options.EnvironmentOverride = EditorGUILayout.TextField(
                new GUIContent("Override Environment", "An explicit environment. If not set, auto " +
                                                       "detects such as 'development', 'production' or 'editor'."),
                Options.EnvironmentOverride);

            GUILayout.Label(new GUIContent(GUIContent.none), EditorStyles.boldLabel);

            GUILayout.Label(new GUIContent(GUIContent.none), EditorStyles.boldLabel);
            Options.Debug = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Debug Mode", "Whether the Sentry SDK should print its diagnostic logs to the console."),
                Options.Debug);
            Options.DebugOnlyInEditor = EditorGUILayout.Toggle(
                new GUIContent(
                    "Only In Editor",
                    "Only print logs when in the editor. Development builds of the player will not include Sentry's SDK diagnostics."),
                Options.DebugOnlyInEditor);
            Options.DiagnosticLevel = (SentryLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Verbosity level", "The minimum level allowed to be printed to the console. " +
                                                  "Log messages with a level below this level are dropped."),
                Options.DiagnosticLevel);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.EndToggleGroup();

            // groupEnabled = EditorGUILayout.BeginToggleGroup("Sentry CLI Options", groupEnabled);
            // uploadSymbols = EditorGUILayout.Toggle("Upload Proguard Mappings", uploadSymbols);
            // auth = EditorGUILayout.TextField("Auth token", auth);
            // organization = EditorGUILayout.TextField("Organization", organization);
            // project = EditorGUILayout.TextField("Project", project);
            // EditorGUILayout.EndToggleGroup();
        }

        private void OnLostFocus()
        {
            Validate();

            EditorUtility.SetDirty(Options);
            AssetDatabase.SaveAssets();
        }

        private void Validate()
        {
            if (!Options.Enabled)
            {
                return;
            }

            ValidateDsn();
        }

        private void ValidateDsn()
        {
            if (string.IsNullOrEmpty(Options.Dsn))
            {
                return;
            }

            if (Uri.IsWellFormedUriString(Options.Dsn, UriKind.Absolute))
            {
                return;
            }

            var fullFieldName = $"{nameof(Options)}.{nameof(Options.Dsn)}";
            var validationError = new ValidationError(fullFieldName, "Invalid DSN format. Expected a URL.");
            OnValidationError(validationError);
            Debug.LogError(validationError.ToString());
        }

        /// <summary>
        /// Creates Sentry folder for storing its configs - Assets/Resources/Sentry
        /// </summary>
        private void CreateConfigDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder($"Assets/Resources/{ScriptableSentryUnityOptions.ConfigRootFolder}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", ScriptableSentryUnityOptions.ConfigRootFolder);
            }

            AssetDatabase.Refresh();
        }

        private void SetTitle()
        {
            var isDarkMode = EditorGUIUtility.isProSkin;
            var texture = new Texture2D(16, 16);
            using var memStream = new MemoryStream();
            using var stream = GetType().Assembly
                .GetManifestResourceStream($"Sentry.Unity.Editor.SentryLogo{(isDarkMode ? "Light" : "Dark")}.png");
            stream.CopyTo(memStream);
            stream.Flush();
            memStream.Position = 0;
            texture.LoadImage(memStream.ToArray());

            titleContent = new GUIContent("Sentry", texture, "Sentry SDK Options");
        }
    }

    public readonly struct ValidationError
    {
        public readonly string PropertyName;

        public readonly string Reason;

        public ValidationError(string propertyName, string reason)
        {
            PropertyName = propertyName;
            Reason = reason;
        }

        public override string ToString()
            => $"[{PropertyName}] Reason: {Reason}";
    }
}
