using System;
using System.Threading;
using UnityEngine;

namespace Sentry.Unity
{
    internal interface ISentrySystemInfo
    {
        int? MainThreadId { get; }
        string? OperatingSystem { get; }
        int? ProcessorCount { get; }
        bool? SupportsVibration { get; }
        string? DeviceType { get; }
        string? CpuDescription { get; }
        string? DeviceName { get; }
        string? DeviceUniqueIdentifier { get; }
        Lazy<string>? DeviceModel { get; }
        int? SystemMemorySize { get; }
        int? GraphicsDeviceId { get; }
        string? GraphicsDeviceName { get; }
        string? GraphicsDeviceVendorId { get; }
        string? GraphicsDeviceVendor { get; }
        int? GraphicsMemorySize { get; }
        bool? GraphicsMultiThreaded { get; }
        string? NpotSupport { get; }
        string? GraphicsDeviceVersion { get; }
        string? GraphicsDeviceType { get; }
        int? MaxTextureSize { get; }
        bool? SupportsDrawCallInstancing { get; }
        bool? SupportsRayTracing { get; }
        bool? SupportsComputeShaders { get; }
        bool? SupportsGeometryShaders { get; }
        int? GraphicsShaderLevel { get; }
    }

    internal sealed class SentrySystemInfoAdapter : ISentrySystemInfo
    {
        public static readonly SentrySystemInfoAdapter Instance = new();

        private SentrySystemInfoAdapter()
        {
        }

        public int? MainThreadId => Thread.CurrentThread.ManagedThreadId;
        public string? OperatingSystem => SystemInfo.operatingSystem;
        public int? ProcessorCount => SystemInfo.processorCount;
        public bool? SupportsVibration => SystemInfo.supportsVibration;
        public string? DeviceType => SystemInfo.deviceType.ToString();
        public string? CpuDescription => SystemInfo.processorType;
        public string? DeviceName => SystemInfo.deviceName;
        public string? DeviceUniqueIdentifier => SystemInfo.deviceUniqueIdentifier;
        public Lazy<string> DeviceModel => new(() => SystemInfo.deviceModel);
        /// <summary>
        /// System memory size in megabytes.
        /// </summary>
        public int? SystemMemorySize => SystemInfo.systemMemorySize;
        public int? GraphicsDeviceId => SystemInfo.graphicsDeviceID;
        public string? GraphicsDeviceName => SystemInfo.graphicsDeviceName;
        public string? GraphicsDeviceVendorId => SystemInfo.graphicsDeviceVendorID.ToString();
        public string? GraphicsDeviceVendor => SystemInfo.graphicsDeviceVendor;
        public int? GraphicsMemorySize => SystemInfo.graphicsMemorySize;
        public bool? GraphicsMultiThreaded => SystemInfo.graphicsMultiThreaded;
        public string? NpotSupport => SystemInfo.npotSupport.ToString();
        public string? GraphicsDeviceVersion => SystemInfo.graphicsDeviceVersion;
        public string? GraphicsDeviceType => SystemInfo.graphicsDeviceType.ToString();
        public int? MaxTextureSize => SystemInfo.maxTextureSize;
        public bool? SupportsDrawCallInstancing => SystemInfo.supportsInstancing;
        public bool? SupportsRayTracing => SystemInfo.supportsRayTracing;
        public bool? SupportsComputeShaders => SystemInfo.supportsComputeShaders;
        public bool? SupportsGeometryShaders => SystemInfo.supportsGeometryShaders;
        public int? GraphicsShaderLevel => SystemInfo.graphicsShaderLevel;
    }
}
