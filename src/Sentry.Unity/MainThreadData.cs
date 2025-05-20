using System;
using System.Threading;

namespace Sentry.Unity;

internal static class MainThreadData
{
    internal static int? MainThreadId { get; set; }

    public static string? OperatingSystem { get; set; }

    public static int? ProcessorCount { get; set; }

    public static bool? SupportsVibration { get; set; }

    public static string? DeviceType { get; set; }

    public static string? CpuDescription { get; set; }

    public static string? DeviceName { get; set; }

    public static string? DeviceUniqueIdentifier { get; set; }

    public static string? DeviceModel { get; set; }

    public static int? SystemMemorySize { get; set; }

    public static int? GraphicsDeviceId { get; set; }

    public static string? GraphicsDeviceName { get; set; }

    public static string? GraphicsDeviceVendorId { get; set; }

    public static string? GraphicsDeviceVendor { get; set; }

    public static int? GraphicsMemorySize { get; set; }

    public static bool? GraphicsMultiThreaded { get; set; }

    public static string? NpotSupport { get; set; }

    public static string? GraphicsDeviceVersion { get; set; }

    public static string? GraphicsDeviceType { get; set; }

    public static int? MaxTextureSize { get; set; }

    public static bool? SupportsDrawCallInstancing { get; set; }

    public static bool? SupportsRayTracing { get; set; }

    public static bool? SupportsComputeShaders { get; set; }

    public static bool? SupportsGeometryShaders { get; set; }

    public static int? GraphicsShaderLevel { get; set; }

    public static bool? IsDebugBuild { get; set; }

    public static string? EditorVersion { get; set; }
    public static string? InstallMode { get; set; }

    public static string? TargetFrameRate { get; set; }

    public static string? CopyTextureSupport { get; set; }

    public static string? RenderingThreadingMode { get; set; }

    public static DateTimeOffset? StartTime { get; set; }

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

    public static void CollectData()
    {
        var sentrySystemInfo = SentrySystemInfo ?? SentrySystemInfoAdapter.Instance;

        // Don't overwrite the MainThreadId if it's already been set
        MainThreadId ??= sentrySystemInfo.MainThreadId;

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
