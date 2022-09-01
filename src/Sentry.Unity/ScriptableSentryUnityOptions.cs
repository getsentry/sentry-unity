using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;
using UnityEngine.Analytics;

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
        [field: SerializeField] public ScreenshotQuality ScreenshotQuality { get; set; } = ScreenshotQuality.High;
        [field: SerializeField] public int ScreenshotCompression { get; set; } = 75;

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
        [field: SerializeField] public float SampleRate { get; set; } = 1.0f;
        [field: SerializeField] public int ShutdownTimeout { get; set; } = 2000;
        [field: SerializeField] public int MaxQueueItems { get; set; } = 30;
        [field: SerializeField] public bool IosNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool AndroidNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool WindowsNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool MacosNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool LinuxNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool Il2CppLineNumberSupportEnabled { get; set; } = false;

        [field: SerializeField] public ScriptableOptionsConfiguration? OptionsConfiguration { get; set; }

        [field: SerializeField] public bool Debug { get; set; } = true;
        [field: SerializeField] public bool DebugOnlyInEditor { get; set; } = true;
        [field: SerializeField] public SentryLevel DiagnosticLevel { get; set; } = SentryLevel.Warning;

        /// <summary>
        /// Loads the ScriptableSentryUnityOptions from `Resource`.
        /// </summary>
        /// <returns>The SentryUnityOptions generated from the ScriptableSentryUnityOptions</returns>
        /// <remarks>
        /// Used for loading the SentryUnityOptions from the ScriptableSentryUnityOptions during runtime.
        /// </remarks>
        public static SentryUnityOptions? LoadSentryUnityOptions(ISentryUnityInfo unityInfo)
        {
            var scriptableOptions = Resources.Load<ScriptableSentryUnityOptions>($"{ConfigRootFolder}/{ConfigName}");
            if (scriptableOptions is not null)
            {
                return scriptableOptions.ToSentryUnityOptions(false, unityInfo);
            }

            return null;
        }

        internal SentryUnityOptions ToSentryUnityOptions(bool isBuilding, ISentryUnityInfo? unityInfo = null, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            var options = new SentryUnityOptions(isBuilding, unityInfo, application)
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
                ScreenshotQuality = ScreenshotQuality,
                ScreenshotCompression = ScreenshotCompression,
                MaxBreadcrumbs = MaxBreadcrumbs,
                ReportAssembliesMode = ReportAssembliesMode,
                SendDefaultPii = SendDefaultPii,
                IsEnvironmentUser = IsEnvironmentUser,
                MaxCacheItems = MaxCacheItems,
                InitCacheFlushTimeout = TimeSpan.FromMilliseconds(InitCacheFlushTimeout),
                SampleRate = SampleRate == 1.0f ? null : SampleRate, // To skip the random check for dropping events
                ShutdownTimeout = TimeSpan.FromMilliseconds(ShutdownTimeout),
                MaxQueueItems = MaxQueueItems,
                // Because SentryOptions.Debug is used inside the .NET SDK to setup the ConsoleLogger we
                // need to set it here directly.
                Debug = ShouldDebug(application.IsEditor && !isBuilding),
                DiagnosticLevel = DiagnosticLevel,
                IosNativeSupportEnabled = IosNativeSupportEnabled,
                AndroidNativeSupportEnabled = AndroidNativeSupportEnabled,
                WindowsNativeSupportEnabled = WindowsNativeSupportEnabled,
                MacosNativeSupportEnabled = MacosNativeSupportEnabled,
                LinuxNativeSupportEnabled = LinuxNativeSupportEnabled,
                Il2CppLineNumberSupportEnabled = Il2CppLineNumberSupportEnabled
            };

            if (!string.IsNullOrWhiteSpace(ReleaseOverride))
            {
                options.Release = ReleaseOverride;
            }

            if (!string.IsNullOrWhiteSpace(EnvironmentOverride))
            {
                options.Environment = EnvironmentOverride;
            }

            options.SetupLogging();

            if (IsKnownPlatform(application))
            {
                options.CacheDirectoryPath = EnableOfflineCaching ? application.PersistentDataPath : null;
            }
            else
            {
                options.DefaultUserId = AnalyticsSessionInfo.userId;

                // This is only provided on a best-effort basis for other than the explicitly supported platforms.
                if (options.BackgroundWorker is null)
                {
                    options.DiagnosticLogger?.LogDebug("Platform support for background thread execution is unknown: using WebBackgroundWorker.");
                    options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
                }

                if (EnableOfflineCaching)
                {
                    options.DiagnosticLogger?.LogDebug("Platform support for offline caching is unknown: disabling.");
                    options.CacheDirectoryPath = null;
                }

                // Requires file access, see https://github.com/getsentry/sentry-unity/issues/290#issuecomment-1163608988
                if (options.AutoSessionTracking)
                {
                    options.DiagnosticLogger?.LogDebug("Platform support for automatic session tracking is unknown: disabling.");
                    options.AutoSessionTracking = false;
                }
            }

            OptionsConfiguration?.Configure(options);

            // Doing this after the configure callback to allow users to programmatically opt out
            if (!isBuilding && options.Il2CppLineNumberSupportEnabled && unityInfo is not null)
            {
                options.AddIl2CppExceptionProcessor(unityInfo);
            }

            return options;
        }

        internal bool ShouldDebug(bool isEditorPlayer)
        {
            if (!isEditorPlayer)
            {
                return !DebugOnlyInEditor && Debug;
            }

            return Debug;
        }

        internal bool IsKnownPlatform(IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;
            return application.Platform
                is RuntimePlatform.Android
                or RuntimePlatform.IPhonePlayer
                or RuntimePlatform.WindowsEditor
                or RuntimePlatform.WindowsPlayer
                or RuntimePlatform.OSXEditor
                or RuntimePlatform.OSXPlayer
                or RuntimePlatform.LinuxEditor
                or RuntimePlatform.LinuxPlayer
                or RuntimePlatform.WebGLPlayer;
        }
    }
}
