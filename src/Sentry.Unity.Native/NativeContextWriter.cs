namespace Sentry.Unity.Native;

internal class NativeContextWriter : ContextWriter
{
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
        ContextWriterUtils.WriteApp(AppStartTime, AppBuildType);

        ContextWriterUtils.WriteOS(OperatingSystemRawDescription);

        ContextWriterUtils.WriteDevice(
            DeviceProcessorCount,
            DeviceCpuDescription,
            DeviceTimezone,
            DeviceSupportsVibration,
            DeviceName,
            DeviceSimulator,
            DeviceDeviceUniqueIdentifier,
            DeviceDeviceType,
            DeviceModel,
            DeviceMemorySize
        );

        ContextWriterUtils.WriteGpu(
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

        ContextWriterUtils.WriteUnity(
            EditorVersion,
            UnityInstallMode,
            UnityTargetFrameRate,
            UnityCopyTextureSupport,
            UnityRenderingThreadingMode);
    }
}
