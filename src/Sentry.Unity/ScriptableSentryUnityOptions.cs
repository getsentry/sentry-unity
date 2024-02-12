using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
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
        [field: SerializeField] public int DebounceTimeLog { get; set; } = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
        [field: SerializeField] public int DebounceTimeWarning { get; set; } = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
        [field: SerializeField] public int DebounceTimeError { get; set; } = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;

        [field: SerializeField] public double TracesSampleRate { get; set; } = 0;
        [field: SerializeField] public bool AutoStartupTraces { get; set; } = true;
        [field: SerializeField] public bool AutoSceneLoadTraces { get; set; } = true;
        [field: SerializeField] public bool AutoAwakeTraces { get; set; } = false;

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

        [field: SerializeField] public bool AttachViewHierarchy { get; set; } = false;
        [field: SerializeField] public int MaxViewHierarchyRootObjects { get; set; } = 100;
        [field: SerializeField] public int MaxViewHierarchyObjectChildCount { get; set; } = 20;
        [field: SerializeField] public int MaxViewHierarchyDepth { get; set; } = 10;

        [field: SerializeField] public bool BreadcrumbsForLogs { get; set; } = true;
        [field: SerializeField] public bool BreadcrumbsForWarnings { get; set; } = true;
        [field: SerializeField] public bool BreadcrumbsForAsserts { get; set; } = true;
        [field: SerializeField] public bool BreadcrumbsForErrors { get; set; } = true;
        [field: SerializeField] public bool BreadcrumbsForExceptions { get; set; } = true;

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

        [field: SerializeField] public bool AnrDetectionEnabled { get; set; } = true;
        [field: SerializeField] public int AnrTimeout { get; set; } = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

        [field: SerializeField] public bool CaptureFailedRequests { get; set; } = true;

        // We hold the status codes as a list of ints to be able to serialize it in the editor.
        [field: SerializeField] public List<int> FailedRequestStatusCodes { get; set; } = new() { 500, 599 };

        [field: SerializeField] public bool FilterBadGatewayExceptions { get; set; } = true;
        [field: SerializeField] public bool FilterWebExceptions { get; set; } = true;
        [field: SerializeField] public bool FilterSocketExceptions { get; set; } = true;

        [field: SerializeField] public bool IosNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool AndroidNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool NdkIntegrationEnabled { get; set; } = true;
        [field: SerializeField] public bool NdkScopeSyncEnabled { get; set; } = true;
        [field: SerializeField] public bool WindowsNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool MacosNativeSupportEnabled { get; set; } = true;
        [field: SerializeField] public bool LinuxNativeSupportEnabled { get; set; } = true;

        [field: SerializeField] public bool Il2CppLineNumberSupportEnabled { get; set; } = true;

        [field: SerializeField] public SentryRuntimeOptionsConfiguration? RuntimeOptionsConfiguration { get; set; }
        [field: SerializeField] public SentryBuildTimeOptionsConfiguration? BuildTimeOptionsConfiguration { get; set; }

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

        internal SentryUnityOptions ToSentryUnityOptions(bool isBuilding, ISentryUnityInfo? unityInfo, IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            var options = new SentryUnityOptions(isBuilding, application)
            {
                Enabled = Enabled,
                Dsn = Dsn,
                CaptureInEditor = CaptureInEditor,
                EnableLogDebouncing = EnableLogDebouncing,
                DebounceTimeLog = TimeSpan.FromMilliseconds(DebounceTimeLog),
                DebounceTimeWarning = TimeSpan.FromMilliseconds(DebounceTimeWarning),
                DebounceTimeError = TimeSpan.FromMilliseconds(DebounceTimeError),
                TracesSampleRate = TracesSampleRate,
                AutoStartupTraces = AutoStartupTraces,
                AutoSceneLoadTraces = AutoSceneLoadTraces,
                AutoSessionTracking = AutoSessionTracking,
                AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(AutoSessionTrackingInterval),
                AttachStacktrace = AttachStacktrace,
                AttachScreenshot = AttachScreenshot,
                ScreenshotQuality = ScreenshotQuality,
                ScreenshotCompression = ScreenshotCompression,
                AttachViewHierarchy = AttachViewHierarchy,
                MaxViewHierarchyRootObjects = MaxViewHierarchyRootObjects,
                MaxViewHierarchyObjectChildCount = MaxViewHierarchyObjectChildCount,
                MaxViewHierarchyDepth = MaxViewHierarchyDepth,
                MaxBreadcrumbs = MaxBreadcrumbs,
                ReportAssembliesMode = ReportAssembliesMode,
                SendDefaultPii = SendDefaultPii,
                IsEnvironmentUser = IsEnvironmentUser,
                MaxCacheItems = MaxCacheItems,
                CacheDirectoryPath = EnableOfflineCaching ? application.PersistentDataPath : null,
                InitCacheFlushTimeout = TimeSpan.FromMilliseconds(InitCacheFlushTimeout),
                SampleRate = SampleRate == 1.0f ? null : SampleRate, // To skip the random check for dropping events
                ShutdownTimeout = TimeSpan.FromMilliseconds(ShutdownTimeout),
                MaxQueueItems = MaxQueueItems,
                // Because SentryOptions.Debug is used inside the .NET SDK to setup the ConsoleLogger we
                // need to set it here directly.
                Debug = ShouldDebug(application.IsEditor && !isBuilding),
                DiagnosticLevel = DiagnosticLevel,
                AnrTimeout = TimeSpan.FromMilliseconds(AnrTimeout),
                CaptureFailedRequests = CaptureFailedRequests,
                FilterBadGatewayExceptions = FilterBadGatewayExceptions,
                IosNativeSupportEnabled = IosNativeSupportEnabled,
                AndroidNativeSupportEnabled = AndroidNativeSupportEnabled,
                NdkIntegrationEnabled = NdkIntegrationEnabled,
                WindowsNativeSupportEnabled = WindowsNativeSupportEnabled,
                MacosNativeSupportEnabled = MacosNativeSupportEnabled,
                LinuxNativeSupportEnabled = LinuxNativeSupportEnabled,
                Il2CppLineNumberSupportEnabled = Il2CppLineNumberSupportEnabled,
                PerformanceAutoInstrumentationEnabled = AutoAwakeTraces,
            };

            if (!string.IsNullOrWhiteSpace(ReleaseOverride))
            {
                options.Release = ReleaseOverride;
            }

            if (!string.IsNullOrWhiteSpace(EnvironmentOverride))
            {
                options.Environment = EnvironmentOverride;
            }

            options.AddBreadcrumbsForLogType[LogType.Log] = BreadcrumbsForLogs;
            options.AddBreadcrumbsForLogType[LogType.Warning] = BreadcrumbsForWarnings;
            options.AddBreadcrumbsForLogType[LogType.Assert] = BreadcrumbsForAsserts;
            options.AddBreadcrumbsForLogType[LogType.Error] = BreadcrumbsForErrors;
            options.AddBreadcrumbsForLogType[LogType.Exception] = BreadcrumbsForExceptions;

            options.FailedRequestStatusCodes = new List<HttpStatusCodeRange>();
            for (var i = 0; i < FailedRequestStatusCodes.Count; i += 2)
            {
                options.FailedRequestStatusCodes.Add(
                    new HttpStatusCodeRange(FailedRequestStatusCodes[i], FailedRequestStatusCodes[i + 1]));
            }

            options.SetupLogging();

            // Bail early if we're building the player.
            if (isBuilding)
            {
                return options;
            }

            if (RuntimeOptionsConfiguration != null)
            {
                // This has to happen in between options object creation and updating the options based on programmatic changes
                RuntimeOptionsConfiguration.Configure(options);
            }

            if (!application.IsEditor && options.Il2CppLineNumberSupportEnabled && unityInfo is not null)
            {
                options.AddIl2CppEventProcessor(unityInfo);
            }

            HandlePlatformRestrictions(options, application, unityInfo);
            HandleExceptionFilter(options);

            if (!AnrDetectionEnabled)
            {
                options.DisableAnrIntegration();
            }

            return options;
        }

        private void HandlePlatformRestrictions(SentryUnityOptions options, IApplication application, ISentryUnityInfo? unityInfo)
        {
            if (unityInfo?.IsKnownPlatform() == false)
            {
                // This is only provided on a best-effort basis for other than the explicitly supported platforms.
                if (options.BackgroundWorker is null)
                {
                    options.DiagnosticLogger?.LogDebug("Platform support for background thread execution is unknown: using WebBackgroundWorker.");
                    options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
                }

                // Disable offline caching regardless whether is was enabled or not.
                options.CacheDirectoryPath = null;
                if (EnableOfflineCaching)
                {
                    options.DiagnosticLogger?.LogDebug("Platform support for offline caching is unknown: disabling.");
                }

                // Requires file access, see https://github.com/getsentry/sentry-unity/issues/290#issuecomment-1163608988
                if (options.AutoSessionTracking)
                {
                    options.DiagnosticLogger?.LogDebug("Platform support for automatic session tracking is unknown: disabling.");
                    options.AutoSessionTracking = false;
                }
            }
        }

        private void HandleExceptionFilter(SentryUnityOptions options)
        {
            if (!options.FilterBadGatewayExceptions)
            {
                options.RemoveExceptionFilter<UnityBadGatewayExceptionFilter>();
            }

            if (!FilterWebExceptions)
            {
                options.RemoveExceptionFilter<UnityWebExceptionFilter>();
            }

            if (!FilterSocketExceptions)
            {
                options.RemoveExceptionFilter<UnitySocketExceptionFilter>();
            }
        }

        internal bool ShouldDebug(bool isEditorPlayer)
        {
            if (!isEditorPlayer)
            {
                return !DebugOnlyInEditor && Debug;
            }

            return Debug;
        }
    }
}
