using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;
using Sentry.Extensibility;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity
{
    /// <summary>
    /// Sentry Unity Options.
    /// </summary>
    /// <remarks>
    /// Options to configure Unity while extending the Sentry .NET SDK functionality.
    /// </remarks>
    public sealed class SentryUnityOptions : SentryOptions
    {
        /// <summary>
        /// UPM name of Sentry Unity SDK (package.json)
        /// </summary>
        public const string PackageName = "io.sentry.unity";

        /// <summary>
        /// Whether the SDK should automatically enable or not.
        /// </summary>
        /// <remarks>
        /// At a minimum, the <see cref="Dsn"/> need to be provided.
        /// </remarks>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Whether Sentry events should be captured while in the Unity Editor.
        /// </summary>
        // Lower entry barrier, likely set to false after initial setup.
        public bool CaptureInEditor { get; set; } = true;

        /// <summary>
        /// Whether Sentry events should be debounced it too frequent.
        /// </summary>
        public bool EnableLogDebouncing { get; set; } = false;

        private CompressionLevelWithAuto _requestBodyCompressionLevel = CompressionLevelWithAuto.Auto;

        /// <summary>
        /// The level which to compress the request body sent to Sentry.
        /// </summary>
        public new CompressionLevelWithAuto RequestBodyCompressionLevel
        {
            get => _requestBodyCompressionLevel;
            set
            {
                _requestBodyCompressionLevel = value;
                if (value == CompressionLevelWithAuto.Auto)
                {
                    // TODO: If WebGL, then NoCompression, else .. optimize (e.g: adapt to platform)
                    // The target platform is known when building the player, so 'auto' should resolve there(here).
                    // Since some platforms don't support GZipping fallback: no compression.
                    base.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
                }
                else
                {
                    // Auto would result in -1 set if not treated before providing the options to the Sentry .NET SDK
                    // DeflateStream would throw System.ArgumentOutOfRangeException
                    base.RequestBodyCompressionLevel = (CompressionLevel)value;
                }
            }
        }

        /// <summary>
        /// Try to attach a current screen capture on error events.
        /// </summary>
        public bool AttachScreenshot { get; set; } = false;

        /// <summary>
        /// The quality of the attached screenshot
        /// </summary>
        public ScreenshotQuality ScreenshotQuality { get; set; } = ScreenshotQuality.High;

        /// <summary>
        /// The JPG compression quality of the attached screenshot
        /// </summary>
        public int ScreenshotCompression { get; set; } = 75;

        /// <summary>
        /// Whether the SDK should automatically add LogType.Debug and LogType.Warning messages as breadcrumbs
        /// </summary>
        public bool addLogsAsBreadcrumbs { get; set; } = true;

        /// <summary>
        /// Whether the SDK should add native support for iOS
        /// </summary>
        public bool IosNativeSupportEnabled { get; set; } = true;

        /// <summary>
        /// Whether the SDK should add native support for Android
        /// </summary>
        public bool AndroidNativeSupportEnabled { get; set; } = true;

        /// <summary>
        /// Whether the SDK should add native support for Windows
        /// </summary>
        public bool WindowsNativeSupportEnabled { get; set; } = true;

        /// <summary>
        /// Whether the SDK should add native support for MacOS
        /// </summary>
        public bool MacosNativeSupportEnabled { get; set; } = true;

        /// <summary>
        /// Whether the SDK should add native support for Linux
        /// </summary>
        public bool LinuxNativeSupportEnabled { get; set; } = true;

        /// <summary>
        /// Whether the SDK should add IL2CPP line number support
        /// </summary>
        /// <remarks>
        /// To give line numbers, Sentry requires the debug symbols Unity generates during build
        /// For that reason, uploading debug information files must be enabled.
        /// For that, Org Slut, Project Slug and Auth token are required.
        /// </remarks>
        public bool Il2CppLineNumberSupportEnabled { get; set; } = true;

        // Initialized by native SDK binding code to set the User.ID in .NET (UnityEventProcessor).
        internal string? _defaultUserId;
        internal string? DefaultUserId
        {
            get => _defaultUserId;
            set
            {
                _defaultUserId = value;
                if (_defaultUserId is null)
                {
                    DiagnosticLogger?.LogWarning("Couldn't set the default user ID - the value is NULL.");
                }
                else
                {
                    DiagnosticLogger?.LogDebug("Setting '{0}' as the default user ID.", _defaultUserId);
                }
            }
        }

        // Whether components & integrations can use multi-threading.
        internal bool MultiThreading = true;

        /// <summary>
        /// Used to synchronize context from .NET to the native SDK
        /// </summary>
        internal ContextWriter? NativeContextWriter { get; set; } = null;

        internal List<string> SdkIntegrationNames { get; set; } = new();

        public SentryUnityOptions() : this(false, null, ApplicationAdapter.Instance) { }

        internal SentryUnityOptions(bool isBuilding, ISentryUnityInfo? unityInfo, IApplication application) :
            this(SentryMonoBehaviour.Instance, application, isBuilding)
        { }

        internal SentryUnityOptions(SentryMonoBehaviour behaviour, IApplication application, bool isBuilding)
        {
            // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
            DetectStartupTime = StartupTimeDetectionMode.Fast;

            this.AddInAppExclude("UnityEngine");
            this.AddInAppExclude("UnityEditor");
            var processor = new UnityEventProcessor(this, behaviour);
            this.AddEventProcessor(processor);
            this.AddTransactionProcessor(processor);

            this.AddIntegration(new UnityLogHandlerIntegration());
            this.AddIntegration(new AnrIntegration(behaviour));
            this.AddIntegration(new UnityScopeIntegration(behaviour, application));
            this.AddIntegration(new UnityBeforeSceneLoadIntegration());
            this.AddIntegration(new SceneManagerIntegration());
            this.AddIntegration(new SessionIntegration(behaviour));

            IsGlobalModeEnabled = true;

            AutoSessionTracking = true;
            RequestBodyCompressionLevel = CompressionLevelWithAuto.NoCompression;
            InitCacheFlushTimeout = System.TimeSpan.Zero;

            // Ben.Demystifer not compatible with IL2CPP
            StackTraceMode = StackTraceMode.Original;
            IsEnvironmentUser = false;

            if (application.ProductName is string productName
                && !string.IsNullOrWhiteSpace(productName)
                && productName.Any(c => c != '.')) // productName consisting solely of '.'
            {
                productName = Regex.Replace(productName, @"\n|\r|\t|\/|\\|\.{2}|@", "_");
                Release = $"{productName}@{application.Version}";
            }
            else
            {
                Release = application.Version;
            }
            if (!string.IsNullOrWhiteSpace(application.BuildGUID))
            {
                Release += $"+{application.BuildGUID}";
            }

            Environment = application.IsEditor && !isBuilding
                ? "editor"
                : "production";
        }

        public override string ToString()
        {
            return $@"Sentry SDK Options:
Capture In Editor: {CaptureInEditor}
Release: {Release}
Environment: {Environment}
Offline Caching: {(CacheDirectoryPath is null ? "disabled" : "enabled")}
";
        }
    }

    /// <summary>
    /// <see cref="CompressionLevel"/> with an additional value for Automatic
    /// </summary>
    public enum CompressionLevelWithAuto
    {
        /// <summary>
        /// The Unity SDK will attempt to choose the best option for the target player.
        /// </summary>
        Auto = -1,
        /// <summary>
        /// The compression operation should be optimally compressed, even if the operation takes a longer time (and CPU) to complete.
        /// Not supported on IL2CPP.
        /// </summary>
        Optimal = CompressionLevel.Optimal,
        /// <summary>
        /// The compression operation should complete as quickly as possible, even if the resulting data is not optimally compressed.
        /// Not supported on IL2CPP.
        /// </summary>
        Fastest = CompressionLevel.Fastest,
        /// <summary>
        /// No compression should be performed.
        /// </summary>
        NoCompression = CompressionLevel.NoCompression,
    }

    /// <summary>
    /// Controls for the JPEG compression quality of the attached screenshot
    /// </summary>
    public enum ScreenshotQuality
    {
        /// <summary>
        /// Full quality
        /// </summary>
        Full,
        /// <summary>
        /// High quality
        /// </summary>
        High,
        /// <summary>
        /// Medium quality
        /// </summary>
        Medium,
        /// <summary>
        /// Low quality
        /// </summary>
        Low
    }
}
