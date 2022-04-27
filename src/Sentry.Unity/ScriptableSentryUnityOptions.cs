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
        [field: SerializeField] public bool CaptureInEditor { get; set; }
        [field: SerializeField] public bool EnableLogDebouncing { get; set; }
        [field: SerializeField] public double TracesSampleRate { get; set; }
        [field: SerializeField] public bool AutoSessionTracking { get; set; }
        /// <summary>
        /// Interval in milliseconds a session terminates if put in the background.
        /// </summary>
        [field: SerializeField] public int AutoSessionTrackingInterval { get; set; }

        [field: SerializeField] public string ReleaseOverride { get; set; } = string.Empty;
        [field: SerializeField] public string EnvironmentOverride { get; set; } = string.Empty;
        [field: SerializeField] public bool AttachStacktrace { get; set; }
        [field: SerializeField] public bool AttachScreenshot { get; set; }
        [field: SerializeField] public int ScreenshotMaxWidth { get; set; }
        [field: SerializeField] public int ScreenshotMaxHeight { get; set; }
        [field: SerializeField] public int ScreenshotQuality { get; set; }
        [field: SerializeField] public int MaxBreadcrumbs { get; set; }
        [field: SerializeField] public ReportAssembliesMode ReportAssembliesMode { get; set; }
        [field: SerializeField] public bool SendDefaultPii { get; set; }
        [field: SerializeField] public bool IsEnvironmentUser { get; set; }

        [field: SerializeField] public bool EnableOfflineCaching { get; set; }
        [field: SerializeField] public int MaxCacheItems { get; set; } = 30;
        [field: SerializeField] public int InitCacheFlushTimeout { get; set; }
        [field: SerializeField] public float? SampleRate { get; set; }
        [field: SerializeField] public int ShutdownTimeout { get; set; }
        [field: SerializeField] public int MaxQueueItems { get; set; } = 30;
        [field: SerializeField] public bool IosNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool AndroidNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool WindowsNativeSupportEnabled { get; set; } = true;

        [field: SerializeField] public ScriptableOptionsConfiguration? OptionsConfiguration { get; set; }

        [field: SerializeField] public bool Debug { get; set; }
        [field: SerializeField] public bool DebugOnlyInEditor { get; set; }
        [field: SerializeField] public SentryLevel DiagnosticLevel { get; set; }

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
                return ToSentryUnityOptions(scriptableOptions, isBuilding);
            }

            return null;
        }

        internal static SentryUnityOptions ToSentryUnityOptions(ScriptableSentryUnityOptions scriptableOptions, bool isBuilding, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            var options = new SentryUnityOptions(application, isBuilding)
            {
                Enabled = scriptableOptions.Enabled,
                Dsn = scriptableOptions.Dsn,
                CaptureInEditor = scriptableOptions.CaptureInEditor,
                EnableLogDebouncing = scriptableOptions.EnableLogDebouncing,
                TracesSampleRate = scriptableOptions.TracesSampleRate,
                AutoSessionTracking = scriptableOptions.AutoSessionTracking,
                AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(scriptableOptions.AutoSessionTrackingInterval),
                AttachStacktrace = scriptableOptions.AttachStacktrace,
                AttachScreenshot = scriptableOptions.AttachScreenshot,
                ScreenshotMaxWidth = scriptableOptions.ScreenshotMaxWidth,
                ScreenshotMaxHeight = scriptableOptions.ScreenshotMaxHeight,
                ScreenshotQuality = scriptableOptions.ScreenshotQuality,
                MaxBreadcrumbs = scriptableOptions.MaxBreadcrumbs,
                ReportAssembliesMode = scriptableOptions.ReportAssembliesMode,
                SendDefaultPii = scriptableOptions.SendDefaultPii,
                IsEnvironmentUser = scriptableOptions.IsEnvironmentUser,
                MaxCacheItems = scriptableOptions.MaxCacheItems,
                InitCacheFlushTimeout = TimeSpan.FromMilliseconds(scriptableOptions.InitCacheFlushTimeout),
                SampleRate = scriptableOptions.SampleRate,
                ShutdownTimeout = TimeSpan.FromMilliseconds(scriptableOptions.ShutdownTimeout),
                MaxQueueItems = scriptableOptions.MaxQueueItems
            };

            if (!string.IsNullOrWhiteSpace(scriptableOptions.ReleaseOverride))
            {
                options.Release = scriptableOptions.ReleaseOverride;
            }

            if (!string.IsNullOrWhiteSpace(scriptableOptions.EnvironmentOverride))
            {
                options.Environment = scriptableOptions.EnvironmentOverride;
            }

            if (!scriptableOptions.EnableOfflineCaching)
            {
                options.CacheDirectoryPath = null;
            }

            options.IosNativeSupportEnabled = scriptableOptions.IosNativeSupportEnabled;
            options.AndroidNativeSupportEnabled = scriptableOptions.AndroidNativeSupportEnabled;
            options.WindowsNativeSupportEnabled = scriptableOptions.WindowsNativeSupportEnabled;

            options.Debug = scriptableOptions.Debug;
            options.DebugOnlyInEditor = scriptableOptions.DebugOnlyInEditor;
            options.DiagnosticLevel = scriptableOptions.DiagnosticLevel;

            SentryOptionsUtility.TryAttachLogger(options);

            scriptableOptions.OptionsConfiguration?.Configure(options);

            return options;
        }
    }
}
