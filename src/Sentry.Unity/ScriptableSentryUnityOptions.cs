using System;
using Sentry.Unity.Integrations;
using Sentry.Unity.Json;
using UnityEngine;

namespace Sentry.Unity
{
    public class ScriptableSentryUnityOptions : ScriptableObject
    {
        /// <summary>
        /// Relative to Assets/Resources
        /// </summary>
        internal const string ConfigRootFolder = "Sentry";

        /// <summary>
        /// Main Sentry config name for Unity
        /// </summary>
        internal const string ConfigName = "SentryOptions";

        /// <summary>
        /// Path for the config for Unity
        /// </summary>
        public static string GetConfigPath(string? notDefaultConfigName = null)
            => $"Assets/Resources/{ConfigRootFolder}/{notDefaultConfigName ?? ConfigName}.asset";

        [field: SerializeField] public bool Enabled { get; set; } = true;

        [field: SerializeField] public string? Dsn { get; set; }
        [field: SerializeField] public bool CaptureInEditor { get; set; } = true;
        [field: SerializeField] public bool EnableLogDebouncing { get; set; } = false;
        [field: SerializeField] public double TracesSampleRate { get; set; } = 0;
        [field: SerializeField] public bool AutoSessionTracking { get; set; } = true;

        /// <summary>
        /// Interval in milliseconds a session terminates if put in the background.
        /// </summary>
        [field: SerializeField] public int AutoSessionTrackingInterval { get; set; } = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;

        [field: SerializeField] public string ReleaseOverride { get; set; } = string.Empty;
        [field: SerializeField] public string EnvironmentOverride { get; set; } = string.Empty;
        [field: SerializeField] public bool AttachStacktrace { get; set; }
        [field: SerializeField] public bool AttachScreenshot { get; set; }
        [field: SerializeField] public int ScreenshotMaxWidth { get; set; }
        [field: SerializeField] public int ScreenshotMaxHeight { get; set; }
        [field: SerializeField] public int ScreenshotQuality { get; set; } = 75;
        [field: SerializeField] public int MaxBreadcrumbs { get; set; } = Constants.DefaultMaxBreadcrumbs;

        [field: SerializeField] public ReportAssembliesMode ReportAssembliesMode { get; set; } = ReportAssembliesMode.Version;
        [field: SerializeField] public bool SendDefaultPii { get; set; }
        [field: SerializeField] public bool IsEnvironmentUser { get; set; }

        [field: SerializeField] public bool EnableOfflineCaching { get; set; } = true;
        [field: SerializeField] public int MaxCacheItems { get; set; } = 30;

        /// <summary>
        /// Time in milliseconds for flushing the cache at startup
        /// </summary>
        [field: SerializeField] public int InitCacheFlushTimeout { get; set; } = (int)TimeSpan.Zero.TotalMilliseconds;
        [field: SerializeField] public float? SampleRate { get; set; }
        [field: SerializeField] public int ShutdownTimeout { get; set; } = 2000;
        [field: SerializeField] public int MaxQueueItems { get; set; } = 30;
        [field: SerializeField] public bool IosNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool AndroidNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool WindowsNativeSupportEnabled { get; set; } = true;

        [field: SerializeField] public ScriptableOptionsConfiguration? OptionsConfiguration { get; set; }

        [field: SerializeField] public bool Debug { get; set; } = true;
        [field: SerializeField] public bool DebugOnlyInEditor { get; set; } = true;
        [field: SerializeField] public SentryLevel DiagnosticLevel { get; set; } = SentryLevel.Warning;

        public static SentryUnityOptions? LoadSentryUnityOptions(bool isBuilding = false)
        {
            // TODO: Deprecated and to be removed once we update far enough.
            var sentryOptionsTextAsset = Resources.Load<TextAsset>($"{ConfigRootFolder}/{ConfigName}");
            if (sentryOptionsTextAsset != null)
            {
                var options = JsonSentryUnityOptions.LoadFromJson(sentryOptionsTextAsset);
                return options;
            }

            var scriptableOptions = Resources.Load<ScriptableSentryUnityOptions>($"{ConfigRootFolder}/{ConfigName}");
            if (scriptableOptions is not null)
            {
                return scriptableOptions.ToSentryUnityOptions(isBuilding);
            }

            return null;
        }

        internal SentryUnityOptions ToSentryUnityOptions(bool isBuilding, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            var options = new SentryUnityOptions(application, isBuilding)
            {
                Enabled = Enabled,
                Dsn = Dsn,
                CaptureInEditor = CaptureInEditor,
                EnableLogDebouncing = EnableLogDebouncing,
                TracesSampleRate = TracesSampleRate,
                AutoSessionTracking = AutoSessionTracking,
                AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(AutoSessionTrackingInterval),
                AttachStacktrace = AttachStacktrace,
                AttachScreenshot = AttachScreenshot,
                ScreenshotMaxWidth = ScreenshotMaxWidth,
                ScreenshotMaxHeight = ScreenshotMaxHeight,
                ScreenshotQuality = ScreenshotQuality,
                MaxBreadcrumbs = MaxBreadcrumbs,
                ReportAssembliesMode = ReportAssembliesMode,
                SendDefaultPii = SendDefaultPii,
                IsEnvironmentUser = IsEnvironmentUser,
                MaxCacheItems = MaxCacheItems,
                InitCacheFlushTimeout = TimeSpan.FromMilliseconds(InitCacheFlushTimeout),
                SampleRate = SampleRate,
                ShutdownTimeout = TimeSpan.FromMilliseconds(ShutdownTimeout),
                MaxQueueItems = MaxQueueItems
            };

            if (!string.IsNullOrWhiteSpace(ReleaseOverride))
            {
                options.Release = ReleaseOverride;
            }

            if (!string.IsNullOrWhiteSpace(EnvironmentOverride))
            {
                options.Environment = EnvironmentOverride;
            }

            if (!EnableOfflineCaching)
            {
                options.CacheDirectoryPath = null;
            }

            options.IosNativeSupportEnabled = IosNativeSupportEnabled;
            options.AndroidNativeSupportEnabled = AndroidNativeSupportEnabled;
            options.WindowsNativeSupportEnabled = WindowsNativeSupportEnabled;

            options.Debug = Debug;
            options.DebugOnlyInEditor = DebugOnlyInEditor;
            options.DiagnosticLevel = DiagnosticLevel;

            options.TryAttachLogger();
            OptionsConfiguration?.Configure(options);

            return options;
        }
    }
}
