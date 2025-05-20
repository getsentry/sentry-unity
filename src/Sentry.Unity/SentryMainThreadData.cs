using System;
using System.Threading;

namespace Sentry.Unity;

public static class SentryMainThreadData
{
    internal static int? MainThreadId { get; set; }

    internal static string? OperatingSystem { get; set; }

    internal static int? ProcessorCount { get; set; }

    internal static bool? SupportsVibration { get; set; }

    internal static string? DeviceType { get; set; }

    internal static string? CpuDescription { get; set; }

    internal static string? DeviceName { get; set; }

    internal static string? DeviceUniqueIdentifier { get; set; }

    internal static string? DeviceModel { get; set; }

    internal static int? SystemMemorySize { get; set; }

    internal static int? GraphicsDeviceId { get; set; }

    internal static string? GraphicsDeviceName { get; set; }

    internal static string? GraphicsDeviceVendorId { get; set; }

    internal static string? GraphicsDeviceVendor { get; set; }

    internal static int? GraphicsMemorySize { get; set; }

    internal static bool? GraphicsMultiThreaded { get; set; }

    internal static string? NpotSupport { get; set; }

    internal static string? GraphicsDeviceVersion { get; set; }

    internal static string? GraphicsDeviceType { get; set; }

    internal static int? MaxTextureSize { get; set; }

    internal static bool? SupportsDrawCallInstancing { get; set; }

    internal static bool? SupportsRayTracing { get; set; }

    internal static bool? SupportsComputeShaders { get; set; }

    internal static bool? SupportsGeometryShaders { get; set; }

    internal static int? GraphicsShaderLevel { get; set; }

    internal static bool? IsDebugBuild { get; set; }

    internal static string? EditorVersion { get; set; }
    internal static string? InstallMode { get; set; }

    internal static string? TargetFrameRate { get; set; }

    internal static string? CopyTextureSupport { get; set; }

    internal static string? RenderingThreadingMode { get; set; }

    internal static DateTimeOffset? StartTime { get; set; }

    internal static bool MainThreadICollected = false;

    public static bool? IsMainThread()
    {
        if (MainThreadId.HasValue)
        {
            return MainThreadId.Equals(Thread.CurrentThread.ManagedThreadId);
        }

        // We don't know whether this is the main thread or not
        return null;
    }

    // For testing
    internal static ISentrySystemInfo? SentrySystemInfo { get; set; }

    public static void Collect()
    {
        var sentrySystemInfo = SentrySystemInfo ?? SentrySystemInfoAdapter.Instance;

        if (!MainThreadICollected)
        {
            MainThreadId = sentrySystemInfo.MainThreadId;
            MainThreadICollected = true;
        }

        ProcessorCount = sentrySystemInfo.ProcessorCount;
        OperatingSystem = sentrySystemInfo.OperatingSystem;
        CpuDescription = sentrySystemInfo.CpuDescription;
        SupportsVibration = sentrySystemInfo.SupportsVibration;
        DeviceName = sentrySystemInfo.DeviceName;
        SystemMemorySize = sentrySystemInfo.SystemMemorySize;
        GraphicsDeviceId = sentrySystemInfo.GraphicsDeviceId;
        GraphicsDeviceName = sentrySystemInfo.GraphicsDeviceName;
        GraphicsDeviceVendor = sentrySystemInfo.GraphicsDeviceVendor;
        GraphicsMemorySize = sentrySystemInfo.GraphicsMemorySize;
        NpotSupport = sentrySystemInfo.NpotSupport;
        GraphicsDeviceVersion = sentrySystemInfo.GraphicsDeviceVersion;
        GraphicsDeviceType = sentrySystemInfo.GraphicsDeviceType;
        MaxTextureSize = sentrySystemInfo.MaxTextureSize;
        SupportsDrawCallInstancing = sentrySystemInfo.SupportsDrawCallInstancing;
        SupportsRayTracing = sentrySystemInfo.SupportsRayTracing;
        SupportsComputeShaders = sentrySystemInfo.SupportsComputeShaders;
        SupportsGeometryShaders = sentrySystemInfo.SupportsGeometryShaders;
        GraphicsShaderLevel = sentrySystemInfo.GraphicsShaderLevel;
        EditorVersion = sentrySystemInfo.EditorVersion;
        InstallMode = sentrySystemInfo.InstallMode;
        DeviceType = sentrySystemInfo.DeviceType?.Value;
        DeviceUniqueIdentifier = sentrySystemInfo.DeviceUniqueIdentifier?.Value;
        DeviceModel = sentrySystemInfo.DeviceModel?.Value;
        GraphicsDeviceVendorId = sentrySystemInfo.GraphicsDeviceVendorId?.Value;
        GraphicsMultiThreaded = sentrySystemInfo.GraphicsMultiThreaded?.Value;
        IsDebugBuild = sentrySystemInfo.IsDebugBuild?.Value;
        TargetFrameRate = sentrySystemInfo.TargetFrameRate?.Value;
        CopyTextureSupport = sentrySystemInfo.CopyTextureSupport?.Value;
        RenderingThreadingMode = sentrySystemInfo.RenderingThreadingMode?.Value;
        StartTime = sentrySystemInfo.StartTime?.Value;
    }
}
