namespace Sentry.Unity.Android.Tests;

internal class TestSentryJava : ISentryJava
{
    public string? InstallationId { get; set; }
    public bool? IsCrashedLastRun { get; set; }

    public void Init(IJniExecutor jniExecutor, string dsn)
    {
        throw new System.NotImplementedException();
    }

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

    public bool IsSentryJavaPresent() => true;
}
