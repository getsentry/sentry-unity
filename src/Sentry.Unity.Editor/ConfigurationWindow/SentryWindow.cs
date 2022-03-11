using System;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Json;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    public class SentryWindow : EditorWindow
    {
        private const string LinkXmlPath = "Assets/Plugins/Sentry/link.xml";

        [MenuItem("Tools/Sentry")]
        public static SentryWindow OpenSentryWindow()
        {
            var window = (SentryWindow)GetWindow(typeof(SentryWindow));
            window.minSize = new Vector2(600, 350);
            return window;
        }

        public static SentryWindow Instance => GetWindow<SentryWindow>();

        protected virtual string SentryOptionsAssetName { get; } = ScriptableSentryUnityOptions.ConfigName;

        public ScriptableSentryUnityOptions Options { get; private set; } = null!; // Set by OnEnable()
        public SentryEditorOptions EditorOptions { get; private set; } = null!; // Set by OnEnable()

        public event Action<ValidationError> OnValidationError = _ => { };

        private int _currentTab = 0;
        private readonly string[] _tabs =
        {
            "Core",
            "Enrichment",
            "Transport",
            "Advanced",
            "Options Config",
            "Debug Symbols"
        };

        private void Awake()
        {
            SetTitle();
            CopyLinkXmlToPlugins();

            CheckForAndConvertJsonConfig();
            Options = LoadOptions();
            EditorOptions = SentryEditorOptions.LoadEditorOptions();
        }

        private ScriptableSentryUnityOptions LoadOptions()
        {
            var options = AssetDatabase.LoadAssetAtPath(
                ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName),
                typeof(ScriptableSentryUnityOptions)) as ScriptableSentryUnityOptions;

            if (options is null)
            {
                options = CreateOptions(SentryOptionsAssetName);
            }

            return options;
        }

        internal static ScriptableSentryUnityOptions CreateOptions(string? notDefaultConfigName = null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder($"Assets/Resources/{ScriptableSentryUnityOptions.ConfigRootFolder}"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", ScriptableSentryUnityOptions.ConfigRootFolder);
            }

            var scriptableOptions = CreateInstance<ScriptableSentryUnityOptions>();
            SentryOptionsUtility.SetDefaults(scriptableOptions);

            AssetDatabase.CreateAsset(scriptableOptions,
                ScriptableSentryUnityOptions.GetConfigPath(notDefaultConfigName));
            AssetDatabase.SaveAssets();

            return scriptableOptions;
        }

        private void CheckForAndConvertJsonConfig()
        {
            var sentryOptionsTextAsset =
                AssetDatabase.LoadAssetAtPath(JsonSentryUnityOptions.GetConfigPath(), typeof(TextAsset)) as TextAsset;
            if (sentryOptionsTextAsset is null)
            {
                // Json config not found, nothing to do.
                return;
            }

            var scriptableOptions = CreateOptions(SentryOptionsAssetName);
            JsonSentryUnityOptions.ToScriptableOptions(sentryOptionsTextAsset, scriptableOptions);

            EditorUtility.SetDirty(scriptableOptions);
            AssetDatabase.SaveAssets();

            AssetDatabase.DeleteAsset(JsonSentryUnityOptions.GetConfigPath());
        }

        // ReSharper disable once UnusedMember.Local
        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("SDK Options", EditorStyles.boldLabel);

            Options.Enabled = EditorGUILayout.ToggleLeft(
                new GUIContent("Enable Sentry", "Controls if the SDK should initialize itself or not."),
                Options.Enabled);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            var selectedTab = GUILayout.Toolbar(_currentTab, _tabs);
            if (selectedTab != _currentTab)
            {
                // Edge-case: Lose focus so currently selected fields don't "bleed" through like DSN -> Override Release
                GUI.FocusControl(null);
                _currentTab = selectedTab;
            }

            EditorGUI.BeginDisabledGroup(!Options.Enabled);
            EditorGUILayout.Space();

            switch (_currentTab)
            {
                case 0:
                    CoreTab.Display(Options);
                    break;
                case 1:
                    EnrichmentTab.Display(Options);
                    break;
                case 2:
                    TransportTab.Display(Options);
                    break;
                case 3:
                    AdvancedTab.Display(Options);
                    break;
                case 4:
                    OptionsConfigurationTab.Display(Options);
                    break;
                case 5:
                    ConfigurationWindow.EditorOptions.Display(EditorOptions);
                    break;
                default:
                    break;
            }

            EditorGUI.EndDisabledGroup();
        }

        private void OnLostFocus()
        {
            // Make sure the actual config asset exists before validating/saving. Crashes the editor otherwise.
            if (!File.Exists(ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName)))
            {
                new UnityLogger(new SentryOptions()).LogWarning("Sentry option could not been saved. " +
                                                                "The configuration asset is missing.");
                return;
            }

            Validate();

            EditorUtility.SetDirty(Options);
            EditorUtility.SetDirty(EditorOptions);
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

        internal void ValidateDsn()
        {
            if (string.IsNullOrWhiteSpace(Options.Dsn))
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

            new UnityLogger(new SentryOptions()).LogWarning(validationError.ToString());
        }

        /// <summary>
        /// Creates Sentry folder 'Plugins/Sentry' and copies the link.xml into it
        /// </summary>
        private void CopyLinkXmlToPlugins()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Plugins"))
            {
                AssetDatabase.CreateFolder("Assets", "Plugins");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Plugins/Sentry"))
            {
                AssetDatabase.CreateFolder("Assets/Plugins", "Sentry");
            }

            if (!File.Exists(LinkXmlPath))
            {
                using var fileStream = File.Create(LinkXmlPath);
                using var resourceStream =
                    GetType().Assembly.GetManifestResourceStream("Sentry.Unity.Editor.Resources.link.xml");
                resourceStream.CopyTo(fileStream);

                AssetDatabase.ImportAsset(LinkXmlPath);
            }
        }

        private void SetTitle()
        {
            var isDarkMode = EditorGUIUtility.isProSkin;
            var texture = new Texture2D(16, 16);
            using var memStream = new MemoryStream();
            using var stream = GetType().Assembly
                .GetManifestResourceStream(
                    $"Sentry.Unity.Editor.Resources.SentryLogo{(isDarkMode ? "Light" : "Dark")}.png");
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
