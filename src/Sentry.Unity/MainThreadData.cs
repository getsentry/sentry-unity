using System;
using System.Threading;

namespace Sentry.Unity;

internal sealed class MainThreadData
{
    internal int? MainThreadId { get; set; }

    public string? OperatingSystem { get; set; }

    public int? ProcessorCount { get; set; }

    public bool? SupportsVibration { get; set; }

    public string? DeviceType { get; set; }

    public string? CpuDescription { get; set; }

    public string? DeviceName { get; set; }

    public string? DeviceUniqueIdentifier { get; set; }

    public string? DeviceModel { get; set; }

    public int? SystemMemorySize { get; set; }

    public int? GraphicsDeviceId { get; set; }

    public string? GraphicsDeviceName { get; set; }

    public string? GraphicsDeviceVendorId { get; set; }

    public string? GraphicsDeviceVendor { get; set; }

    public int? GraphicsMemorySize { get; set; }

    public bool? GraphicsMultiThreaded { get; set; }

    public string? NpotSupport { get; set; }

    public string? GraphicsDeviceVersion { get; set; }

    public string? GraphicsDeviceType { get; set; }

    public int? MaxTextureSize { get; set; }

    public bool? SupportsDrawCallInstancing { get; set; }

    public bool? SupportsRayTracing { get; set; }

    public bool? SupportsComputeShaders { get; set; }

    public bool? SupportsGeometryShaders { get; set; }

    public int? GraphicsShaderLevel { get; set; }

    public bool? IsDebugBuild { get; set; }

    public string? EditorVersion { get; set; }
    public string? InstallMode { get; set; }

    public string? TargetFrameRate { get; set; }

    public string? CopyTextureSupport { get; set; }

    public string? RenderingThreadingMode { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public bool IsMainThread()
        => MainThreadId.HasValue && Thread.CurrentThread.ManagedThreadId == MainThreadId;
}