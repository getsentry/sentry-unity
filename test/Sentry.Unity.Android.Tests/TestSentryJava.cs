namespace Sentry.Unity.Android.Tests
{
    internal class TestSentryJava : ISentryJava
    {
        public string? InstallationId { get; set; }
        public bool? IsCrashedLastRun { get; set; }

        public string? GetInstallationId(IJniExecutor jniExecutor)
        {
            return InstallationId;
        }

        public bool? CrashedLastRun(IJniExecutor jniExecutor)
        {
            return IsCrashedLastRun;
        }

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
}
