using System;
using System.IO;
using Sentry.Extensibility;
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

        protected virtual string SentryOptionsAssetName { get; } = UnitySentryOptions.ConfigName;

        // Will be used only from Unity Editor
        protected string SentryOptionsAssetPath
            => $"{Application.dataPath}/Resources/{UnitySentryOptions.ConfigRootFolder}/{SentryOptionsAssetName}.json";

        public UnitySentryOptions Options { get; set; } = null!; // Set by OnEnable()

        public event Action<ValidationError> OnValidationError = _ => { };

        private void OnEnable()
        {
            SetTitle();

            TryCreateSentryFolder();

            Options = LoadUnitySentryOptions();

            TryCopyLinkXml(Options.DiagnosticLogger);
        }

        private UnitySentryOptions LoadUnitySentryOptions()
        {
            if (File.Exists(SentryOptionsAssetPath))
            {
                return UnitySentryOptions.LoadFromUnity();
            }

            var unitySentryOptions = new UnitySentryOptions { Enabled = true };
            unitySentryOptions
                .TryAttachLogger()
                .SaveToUnity(SentryOptionsAssetPath);

            return unitySentryOptions;
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
            if (Options.Dsn == null)
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
        /// Called if window focus is lost or 'Close' is called
        /// </summary>
        public void OnLostFocus()
        {
            Validate();

            Options.SaveToUnity(SentryOptionsAssetPath);
            AssetDatabase.Refresh();
        }

        private bool _open;
        private CompressionLevel _content;

        // ReSharper disable once UnusedMember.Local
        private void OnGUI()
        {
            GUILayout.Label(new GUIContent(GUIContent.none), EditorStyles.boldLabel);
            Options.DisableProgrammaticInitialization = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Disable Programmatic Initialization", "Disable manual Sentry setup. Rely on SentryOptions config."),
                Options.DisableProgrammaticInitialization);

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
                Options.SampleRate ?? 1.0f, 0.01f, 1);
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
            Options.DisableAutoCompression = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Disable Auto Compress Payload", "Disable auto Sentry setup. Rely on SentryOptions config."),
                Options.DisableAutoCompression);
            Options.RequestBodyCompressionLevel = (CompressionLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Compress Payload", "The URL to your project inside Sentry. Get yours in Sentry, Project Settings."),
                Options.RequestBodyCompressionLevel);
            EditorGUILayout.EndToggleGroup();

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

            EditorGUILayout.EndToggleGroup();

            // groupEnabled = EditorGUILayout.BeginToggleGroup("Sentry CLI Options", groupEnabled);
            // uploadSymbols = EditorGUILayout.Toggle("Upload Proguard Mappings", uploadSymbols);
            // auth = EditorGUILayout.TextField("Auth token", auth);
            // organization = EditorGUILayout.TextField("Organization", organization);
            // project = EditorGUILayout.TextField("Project", project);
            // EditorGUILayout.EndToggleGroup();
        }

        /// <summary>
        /// Creates Sentry folder for storing its configs - Assets/Resources/Sentry
        /// </summary>
        private static void TryCreateSentryFolder()
        {
            // TODO: revise, 'Resources' is a special Unity folder which is created by default. Not sure this check is needed.
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder($"Assets/Resources/{UnitySentryOptions.ConfigRootFolder}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", UnitySentryOptions.ConfigRootFolder);
            }
        }

        /// <summary>
        /// Find and copy 'link.xml' into current Unity project for IL2CPP builds
        /// </summary>
        private static void TryCopyLinkXml(IDiagnosticLogger? logger)
        {
            const string linkXmlFileName = "link.xml";

            var linkXmlPath = $"{Application.dataPath}/Resources/{UnitySentryOptions.ConfigRootFolder}/{linkXmlFileName}";
            if (File.Exists(linkXmlPath))
            {
                return;
            }

            logger?.Log(SentryLevel.Debug, $"'{linkXmlFileName}' is not found. Creating one!");

            var linkPath = GetLinkXmlPath(linkXmlFileName);
            if (linkPath == null)
            {
                logger?.Log(SentryLevel.Fatal, $"Couldn't locate '{linkXmlFileName}' in 'Packages'.");
                return;
            }

            var linkXmlAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(linkPath);
            File.WriteAllBytes(linkXmlPath, linkXmlAsset.bytes);
        }

        /// <summary>
        /// Get Unity path to 'link.xml' file from `Packages` folder.
        ///
        /// Release UPM:
        ///   Given:   link.xml
        ///   Returns: Packages/io.sentry.unity/Runtime/link.xml
        ///
        /// Dev UPM:
        ///   Given:   link.xml
        ///   Returns: Packages/io.sentry.unity.dev/Runtime/link.xml
        /// </summary>
        private static string? GetLinkXmlPath(string linkXmlFileName)
        {
            var assetIds = AssetDatabase.FindAssets(UnitySentryOptions.PackageName, new [] { "Packages" });
            for (var i = 0; i < assetIds.Length; i++)
            {
                var assetName = AssetDatabase.GUIDToAssetPath(assetIds[i]);
                if (assetName.Contains("Runtime"))
                {
                    var linkFolderPath = Path.GetDirectoryName(assetName)!;
                    return Path.Combine(linkFolderPath, linkXmlFileName);
                }
            }

            return null;
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
