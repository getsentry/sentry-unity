using CWUtil = Sentry.Unity.NativeUtils.ContextWriter;

namespace Sentry.Unity.Android
{
    internal class NativeContextWriter : ContextWriter
    {
        private readonly IJniExecutor _jniExecutor;
        private readonly ISentryJava _sentryJava;

        public NativeContextWriter(IJniExecutor jniExecutor, ISentryJava sentryJava)
        {
            _jniExecutor = jniExecutor;
            _sentryJava = sentryJava;
        }

        protected override void WriteScope(
            string? AppStartTime,
            string? AppBuildType,
            string? OperatingSystemRawDescription,
            int? DeviceProcessorCount,
            string? DeviceCpuDescription,
            string? DeviceTimezone,
            bool? DeviceSupportsVibration,
            string? DeviceName,
            bool? DeviceSimulator,
            string? DeviceDeviceUniqueIdentifier,
            string? DeviceDeviceType,
            string? DeviceModel,
            long? DeviceMemorySize,
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
            string? GpuGraphicsShaderLevel,
            string? EditorVersion,
            string? UnityInstallMode,
            string? UnityTargetFrameRate,
            string? UnityCopyTextureSupport,
            string? UnityRenderingThreadingMode
        )
        {
            // We're only setting the missing contexts, the rest is configured by sentry-java.  We could also sync
            // the "unity" context, but it doesn't seem so useful and the effort to do is larger because there's no
            // class for it in Java - not sure how we could add a generic context object in Java...
            _sentryJava.WriteScope(
                _jniExecutor,
                GpuId,
                GpuName,
                GpuVendorName,
                GpuMemorySize,
                GpuNpotSupport,
                GpuVersion,
                GpuApiType,
                GpuMaxTextureSize,
                GpuSupportsDrawCallInstancing,
                GpuSupportsRayTracing,
                GpuSupportsComputeShaders,
                GpuSupportsGeometryShaders,
                GpuVendorId,
                GpuMultiThreadedRendering,
                GpuGraphicsShaderLevel);

            CWUtil.WriteGpu(
                GpuId,
                GpuName,
                GpuVendorName,
                GpuMemorySize,
                GpuNpotSupport,
                GpuVersion,
                GpuApiType,
                GpuMaxTextureSize,
                GpuSupportsDrawCallInstancing,
                GpuSupportsRayTracing,
                GpuSupportsComputeShaders,
                GpuSupportsGeometryShaders,
                GpuVendorId,
                GpuMultiThreadedRendering,
                GpuGraphicsShaderLevel);

            CWUtil.WriteUnity(
                EditorVersion,
                UnityInstallMode,
                UnityTargetFrameRate,
                UnityCopyTextureSupport,
                UnityRenderingThreadingMode);
        }
    }
}
