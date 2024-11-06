namespace Sentry.Unity;

public partial class SentryUnityOptions
{
    /// <summary>
    /// Exposes native options.
    /// </summary>
    public NativeOptions Native { get; } = new();

    public class NativeOptions
    {
        public bool Enabled { get; set; } = true;
        public string? Dsn { get; set; }
        public bool Debug { get; set; }
        public SentryLevel DiagnosticLevel { get; set; }
        public int MaxBreadcrumb { get; set; }
        public int MaxCacheItem { get; set; }
        public bool EnableCaptureFailedRequest { get; set; }
        public string? FailedRequestStatusCodes { get; set; }
        public bool SendDefaultPii { get; set; }
        public bool AttachScreenshot { get; set; }
        public string? Release { get; set; }
        public string? Environment { get; set; }
        public float SampleRate { get; set; }

        // public bool EnableNetworkBreadcrumbs { get; set; }
        // public string EnableAutoSessionTracking { get; set; }
        // public string EnableAppHangTracking { get; set; }

        public AndroidOptions AndroidOptions { get; set; } = new();

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
    }

    public class AndroidOptions
    {
        public bool NdkIntegrationEnabled { get; set; }
        public bool NdkScopeSyncEnabled { get; set; }

    }
}
