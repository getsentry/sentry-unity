namespace Sentry.Unity.NativeUtils
{
    internal static class ContextWriter
    {
        internal static void WriteApp(string? AppStartTime, string? AppBuildType)
        {
            var obj = C.sentry_value_new_object();
            C.setValueIfNotNull(obj, "app_start_time", AppStartTime);
            C.setValueIfNotNull(obj, "build_type", AppBuildType);
            C.sentry_set_context(Sentry.Protocol.App.Type, obj);
        }

        internal static void WriteOS(string? OperatingSystemRawDescription)
        {
            var obj = C.sentry_value_new_object();
            C.setValueIfNotNull(obj, "raw_description", OperatingSystemRawDescription);
            C.sentry_set_context(Sentry.Protocol.OperatingSystem.Type, obj);
        }

        internal static void WriteDevice(
            int? DeviceProcessorCount,
            string? DeviceCpuDescription,
            string? DeviceTimezone,
            bool? DeviceSupportsVibration,
            string? DeviceName,
            bool? DeviceSimulator,
            string? DeviceDeviceUniqueIdentifier,
            string? DeviceDeviceType,
            string? DeviceModel,
            long? DeviceMemorySize)
        {
            var obj = C.sentry_value_new_object();
            C.setValueIfNotNull(obj, "processor_count", DeviceProcessorCount);
            C.setValueIfNotNull(obj, "cpu_description", DeviceCpuDescription);
            C.setValueIfNotNull(obj, "timezone", DeviceTimezone);
            C.setValueIfNotNull(obj, "supports_vibration", DeviceSupportsVibration);
            C.setValueIfNotNull(obj, "name", DeviceName);
            C.setValueIfNotNull(obj, "simulator", DeviceSimulator);
            C.setValueIfNotNull(obj, "device_unique_identifier", DeviceDeviceUniqueIdentifier);
            C.setValueIfNotNull(obj, "device_type", DeviceDeviceType);
            C.setValueIfNotNull(obj, "model", DeviceModel);
            C.setValueIfNotNull(obj, "memory_size", DeviceMemorySize);
            C.sentry_set_context(Sentry.Protocol.Device.Type, obj);
        }

        internal static void WriteGpu(
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
        {
            var obj = C.sentry_value_new_object();
            C.setValueIfNotNull(obj, "id", GpuId);
            C.setValueIfNotNull(obj, "name", GpuName);
            C.setValueIfNotNull(obj, "vendor_name", GpuVendorName);
            C.setValueIfNotNull(obj, "memory_size", GpuMemorySize);
            C.setValueIfNotNull(obj, "npot_support", GpuNpotSupport);
            C.setValueIfNotNull(obj, "version", GpuVersion);
            C.setValueIfNotNull(obj, "api_type", GpuApiType);
            C.setValueIfNotNull(obj, "max_texture_size", GpuMaxTextureSize);
            C.setValueIfNotNull(obj, "supports_draw_call_instancing", GpuSupportsDrawCallInstancing);
            C.setValueIfNotNull(obj, "supports_ray_tracing", GpuSupportsRayTracing);
            C.setValueIfNotNull(obj, "supports_compute_shaders", GpuSupportsComputeShaders);
            C.setValueIfNotNull(obj, "supports_geometry_shaders", GpuSupportsGeometryShaders);
            C.setValueIfNotNull(obj, "vendor_id", GpuVendorId);
            C.setValueIfNotNull(obj, "multi_threaded_rendering", GpuMultiThreadedRendering);
            C.setValueIfNotNull(obj, "graphics_shader_level", GpuGraphicsShaderLevel);
            C.sentry_set_context(Sentry.Protocol.Gpu.Type, obj);
        }

        internal static void WriteUnity(
            string? UnityInstallMode,
            string? UnityTargetFrameRate,
            string? UnityCopyTextureSupport,
            string? UnityRenderingThreadingMode)
        {
            var obj = C.sentry_value_new_object();
            C.setValueIfNotNull(obj, "install_mode", UnityInstallMode);
            C.setValueIfNotNull(obj, "target_frame_rate", UnityTargetFrameRate);
            C.setValueIfNotNull(obj, "copy_texture_support", UnityCopyTextureSupport);
            C.setValueIfNotNull(obj, "rendering_threading_mode", UnityRenderingThreadingMode);
            C.sentry_set_context(Sentry.Unity.Protocol.Unity.Type, obj);
        }
    }
}
