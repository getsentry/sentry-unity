using System.Linq;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;
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
        /// Maximum width of the screenshot or 0 to keep the original size.
        /// If the application window is larger, the screenshot will be resized proportionally.
        /// </summary>
        public int ScreenshotMaxWidth { get; set; } = 0;

        /// <summary>
        /// Maximum height of the screenshot or 0 to keep the original size.
        /// If the application window is larger, the screenshot will be resized proportionally.
        /// </summary>
        public int ScreenshotMaxHeight { get; set; } = 0;

        /// <summary>
        /// Quality of the JPG screenshot: 0 - 100, where 100 is the best quality and highest size.
        /// </summary>
        public int ScreenshotQuality { get; set; } = 75;

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


        // Initialized by native SDK binding code to set the User.ID in .NET (UnityEventProcessor).
        internal string? DefaultUserId;

        public SentryUnityOptions() : this(ApplicationAdapter.Instance, false)
        {
        }

        internal SentryUnityOptions(IApplication application, bool isBuilding)
        {
            // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
            DetectStartupTime = StartupTimeDetectionMode.Fast;

            this.AddInAppExclude("UnityEngine");
            this.AddInAppExclude("UnityEditor");
            this.AddEventProcessor(new UnityEventProcessor(this, SentryMonoBehaviour.Instance));
            this.AddExceptionProcessor(new UnityEventExceptionProcessor());
            this.AddIntegration(new UnityApplicationLoggingIntegration());
            this.AddIntegration(new UnityBeforeSceneLoadIntegration());
            this.AddIntegration(new SceneManagerIntegration());
            this.AddIntegration(new SessionIntegration(SentryMonoBehaviour.Instance));

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

            Environment = (application.IsEditor && !isBuilding)
                ? "editor"
                : "production";

            CacheDirectoryPath = application.PersistentDataPath;
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
}
