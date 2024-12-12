namespace Sentry.Unity.Android.Tests;

internal class TestSentryJava : ISentryJava
{
    public bool Enabled { get; set; } = true;
    public bool InitSuccessful { get; set; } = true;
    public bool SentryPresent { get; set; } = true;
    public string? InstallationId { get; set; }
    public bool? IsCrashedLastRun { get; set; }

    public bool IsEnabled(IJniExecutor jniExecutor) => Enabled;

    public bool Init(IJniExecutor jniExecutor, SentryUnityOptions options) => InitSuccessful;

    public string? GetInstallationId(IJniExecutor jniExecutor) => InstallationId;

    public bool? CrashedLastRun(IJniExecutor jniExecutor) => IsCrashedLastRun;

    public void Close(IJniExecutor jniExecutor) { }

    public void WriteScope(
        IJniExecutor jniExecutor,
        int? GpuId,
        string? GpuName,
        string? GpuVendorName,
        int? GpuMemorySize,
        string? GpuNpotSupport,
        string? GpuVersion,
        string? GpuApiType,
        int? GpuMaxTextureSize,
        bool? GpuSupportsDrawCallInstancing,
        bool? GpuSupportsRayTracing,
        bool? GpuSupportsComputeShaders,
        bool? GpuSupportsGeometryShaders,
        string? GpuVendorId,
        bool? GpuMultiThreadedRendering,
        string? GpuGraphicsShaderLevel)
    { }

    public bool IsSentryJavaPresent() => SentryPresent;
}
