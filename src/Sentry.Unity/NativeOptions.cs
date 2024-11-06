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
    }
}
