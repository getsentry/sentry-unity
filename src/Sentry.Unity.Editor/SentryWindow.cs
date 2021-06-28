using System;
using System.IO;
using Sentry.Extensibility;
using Sentry.Unity.Json;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryWindow : EditorWindow
    {
        private const string LinkXmlPath = "Assets/Plugins/Sentry/link.xml";

        [MenuItem("Tools/Sentry")]
        public static SentryWindow OpenSentryWindow()
            => (SentryWindow)GetWindow(typeof(SentryWindow));

        protected virtual string SentryOptionsAssetName { get; } = ScriptableSentryUnityOptions.ConfigName;

        public ScriptableSentryUnityOptions Options { get; private set; } = null!; // Set by OnEnable()

        public event Action<ValidationError> OnValidationError = _ => { };

        private int _currentTab = 0;
        private string[] _tabs = new[] {"Core", "Enrichment", "Transport", "Debug"};

        private void OnEnable()
        {
            SetTitle();
            CopyLinkXmlToPlugins();

            CheckForAndConvertJsonConfig();
            Options = LoadOptions();
        }

        private ScriptableSentryUnityOptions LoadOptions()
        {
            var options = AssetDatabase.LoadAssetAtPath(
                ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName), typeof(ScriptableSentryUnityOptions)) as ScriptableSentryUnityOptions;

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

            AssetDatabase.CreateAsset(scriptableOptions, ScriptableSentryUnityOptions.GetConfigPath(notDefaultConfigName));
            AssetDatabase.SaveAssets();

            return scriptableOptions;
        }

        private void CheckForAndConvertJsonConfig()
        {
            var sentryOptionsTextAsset = AssetDatabase.LoadAssetAtPath(JsonSentryUnityOptions.GetConfigPath(), typeof(TextAsset)) as TextAsset;
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

            _currentTab = GUILayout.Toolbar(_currentTab, _tabs);
            EditorGUI.BeginDisabledGroup(!Options.Enabled);
            EditorGUILayout.Space();

            switch (_currentTab)
            {
                case 0:
                    DisplayCore();
                    break;
                case 1:
                    DisplayEnrichment();
                    break;
                case 2:
                    DisplayTransport();
                    break;
                case 3:
                    DisplayDebug();
                    break;
                default:
                    break;
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DisplayCore()
        {
            GUILayout.Label("Base Options", EditorStyles.boldLabel);

            var dsn = Options.Dsn;
            dsn = EditorGUILayout.TextField(
                new GUIContent("DSN", "The URL to your Sentry project. " +
                                      "Get yours on sentry.io -> Project Settings."),
                dsn);
            if (!string.IsNullOrWhiteSpace(dsn))
            {
                Options.Dsn = dsn;
            }

            Options.CaptureInEditor = EditorGUILayout.Toggle(
                new GUIContent("Capture In Editor", "Capture errors while running in the Editor."),
                Options.CaptureInEditor);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            GUILayout.Label("Transactions", EditorStyles.boldLabel);

            var traceSampleRate = (float?)Options.TracesSampleRate;
            Options.TracesSampleRate = EditorGUILayout.Slider(
                new GUIContent("Trace Sample Rate", "Indicates the percentage of the transactions that is " +
                                                    "collected. Setting this to 0 discards all trace data. " +
                                                    "Setting this to 1.0 collects all trace data."),
                traceSampleRate ??= 0.0f, 0.0f, 1.0f);
            if (traceSampleRate > 0.0f)
            {
                Options.TracesSampleRate = (double)traceSampleRate;
            }

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            Options.AutoSessionTracking = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Auto Session Tracking", "Whether the SDK should start a session " +
                                                        "automatically when it's initialized and end the session " +
                                                        "when it's closed."),
                Options.AutoSessionTracking);

            var autoSessionTrackingInterval = EditorGUILayout.DoubleField(
                new GUIContent("Session Timeout [ms]", "The duration of time a session can stay paused " +
                                                       "before it's considered ended."),
                Options.AutoSessionTrackingInterval.TotalMilliseconds);
            Options.AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(autoSessionTrackingInterval);

            EditorGUILayout.EndToggleGroup();
        }

        private void DisplayEnrichment()
        {
            GUILayout.Label("Tag Overrides", EditorStyles.boldLabel);

            Options.ReleaseOverride = EditorGUILayout.TextField(
                new GUIContent("Override Release", "By default release is built from " +
                                                   "'Application.productName'@'Application.version'. " +
                                                   "This option is an override."),
                Options.ReleaseOverride);

            Options.EnvironmentOverride = EditorGUILayout.TextField(
                new GUIContent("Override Environment", "Auto detects 'production' or 'editor' by " +
                                                       "default based on 'Application.isEditor." +
                                                       "\nThis option is an override."),
                Options.EnvironmentOverride);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            GUILayout.Label("Stacktrace", EditorStyles.boldLabel);

            Options.AttachStacktrace = EditorGUILayout.Toggle(
                new GUIContent("Stacktrace For Logs", "Whether to include a stack trace for non " +
                                                      "error events like logs. Even when Unity didn't include and no " +
                                                      "exception was thrown. Refer to AttachStacktrace on sentry docs."),
                Options.AttachStacktrace);

            // Options.StackTraceMode = (StackTraceMode) EditorGUILayout.EnumPopup(
            //     new GUIContent("Stacktrace Mode", "Enhanced is the default." +
            //                                       "\n - Enhanced: Include async, return type, args,..." +
            //                                       "\n - Original - Default .NET stack trace format."),
            //     Options.StackTraceMode);

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            Options.SendDefaultPii = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Send default Pii", "Whether to include default Personal Identifiable " +
                                                   "Information."),
                Options.SendDefaultPii);

            Options.IsEnvironmentUser = EditorGUILayout.Toggle(
                new GUIContent("Auto Set UserName", "Whether to report the 'Environment.UserName' as " +
                                                            "the User affected in the event. Should be disabled for " +
                                                            "Android and iOS."),
                Options.IsEnvironmentUser);

            Options.ServerNameOverride = EditorGUILayout.TextField(
                new GUIContent("Server Name Override", "The name of the server running the application." +
                                                       "\nThis option is an override."),
                Options.ServerNameOverride);

            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            Options.MaxBreadcrumbs = EditorGUILayout.IntField(
                new GUIContent("Max Breadcrumbs", "Maximum number of breadcrumbs that get captured." +
                                                  "\nDefault: 100"),
                Options.MaxBreadcrumbs);

            Options.ReportAssembliesMode = (ReportAssembliesMode)EditorGUILayout.EnumPopup(
                new GUIContent("Report Assemblies","Whether or not to include referenced assemblies " +
                                                   "in each event sent to sentry."),
                Options.ReportAssembliesMode);
        }

        private void DisplayTransport()
        {
            Options.EnableOfflineCaching = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Enable Offline Caching", ""),
                Options.EnableOfflineCaching);
            Options.MaxCacheItems = EditorGUILayout.IntField(
                new GUIContent("Max Cache Items", "The maximum number of items to keep in cache. SDK " +
                                                  "deletes the oldest and migrates the sessions to the next to " +
                                                  "maintain the integrity of your release health stats.\nDefault: 30"),
                Options.MaxCacheItems);

            var initCacheFlushTimeout = EditorGUILayout.DoubleField(
                new GUIContent("Init Flush Timeout [ms]", "The timeout that limits how long the SDK " +
                                                          "will attempt to flush existing cache during initialization."),
                Options.InitCacheFlushTimeout.TotalMilliseconds);
            Options.InitCacheFlushTimeout = TimeSpan.FromMilliseconds(initCacheFlushTimeout);

            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
            EditorGUILayout.Space();

            // Options.RequestBodyCompressionLevel = (CompressionLevelWithAuto)EditorGUILayout.EnumPopup(
            //     new GUIContent("Compress Payload", "The level of which to compress the Sentry event " +
            //                                        "before sending to Sentry."),
            //     Options.RequestBodyCompressionLevel);

            var sampleRate = Options.SampleRate ??= 1.0f;
            sampleRate = EditorGUILayout.Slider(
                new GUIContent("Event Sample Rate", "Indicates the percentage of events that are " +
                                                    "captured. Setting this to 0.1 captures 10% of events. " +
                                                    "Setting this to 1.0 captures all events. Events are picked randomly."),
                sampleRate, 0.01f, 1);
            if (sampleRate < 1.0f)
            {
                Options.SampleRate = sampleRate;
            }

            var shutDownTimeout = EditorGUILayout.DoubleField(
                new GUIContent("Shut Down Timeout [ms]", "How many seconds to wait before shutting down to " +
                                                    "give Sentry time to send events from the background queue."),
                Options.ShutdownTimeout.TotalMilliseconds);
            Options.ShutdownTimeout = TimeSpan.FromMilliseconds(shutDownTimeout);

            Options.MaxQueueItems = EditorGUILayout.IntField(
                new GUIContent("Max Queue Items", "The maximum number of events to keep while the " +
                                                  "worker attempts to send them."),
                Options.MaxQueueItems
            );
        }

        private void DisplayDebug()
        {
            Options.Debug = EditorGUILayout.BeginToggleGroup(
                new GUIContent("Enable Debug Output", "Whether the Sentry SDK should print its " +
                                                      "diagnostic logs to the console."),
                Options.Debug);

            Options.DebugOnlyInEditor = EditorGUILayout.Toggle(
                new GUIContent("Only In Editor", "Only print logs when in the editor. Development " +
                                                 "builds of the player will not include Sentry's SDK diagnostics."),
                Options.DebugOnlyInEditor);

            Options.DiagnosticLevel = (SentryLevel)EditorGUILayout.EnumPopup(
                new GUIContent("Verbosity level", "The minimum level allowed to be printed to the console. " +
                                                  "Log messages with a level below this level are dropped."),
                Options.DiagnosticLevel);

            EditorGUILayout.EndToggleGroup();
        }

        private void OnLostFocus()
        {
            if (Options is null)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName)))
            {
                return;
            }

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

            if (!AssetDatabase.IsValidFolder(LinkXmlPath))
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
                .GetManifestResourceStream($"Sentry.Unity.Editor.Resources.SentryLogo{(isDarkMode ? "Light" : "Dark")}.png");
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
