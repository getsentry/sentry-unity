using System;
using System.Threading;
using UnityEngine;

namespace Sentry.Unity
{
    // Warning: The `Lazy<>` properties may only be evaluated on the main (UI) thread.
    internal interface ISentrySystemInfo
    {
        int? MainThreadId { get; }
        string? OperatingSystem { get; }
        int? ProcessorCount { get; }
        bool? SupportsVibration { get; }
        Lazy<string>? DeviceType { get; }
        string? CpuDescription { get; }
        string? DeviceName { get; }
        Lazy<string>? DeviceUniqueIdentifier { get; }
        Lazy<string>? DeviceModel { get; }
        int? SystemMemorySize { get; }
        int? GraphicsDeviceId { get; }
        string? GraphicsDeviceName { get; }
        Lazy<string>? GraphicsDeviceVendorId { get; }
        string? GraphicsDeviceVendor { get; }
        int? GraphicsMemorySize { get; }
        Lazy<bool>? GraphicsMultiThreaded { get; }
        string? NpotSupport { get; }
        string? GraphicsDeviceVersion { get; }
        string? GraphicsDeviceType { get; }
        int? MaxTextureSize { get; }
        bool? SupportsDrawCallInstancing { get; }
        bool? SupportsRayTracing { get; }
        bool? SupportsComputeShaders { get; }
        bool? SupportsGeometryShaders { get; }
        int? GraphicsShaderLevel { get; }
        bool? GraphicsUVStartsAtTop { get; }
        Lazy<bool>? IsDebugBuild { get; }
        string? EditorVersion { get; }
        string? InstallMode { get; }
        Lazy<string>? TargetFrameRate { get; }
        Lazy<string>? CopyTextureSupport { get; }
        Lazy<string>? RenderingThreadingMode { get; }
        Lazy<DateTimeOffset>? StartTime { get; }
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
        public Lazy<string>? DeviceType => new(() => SystemInfo.deviceType.ToString());
        public string? CpuDescription => SystemInfo.processorType;
        public string? DeviceName
        {
            get
            {
                // Workaround for https://github.com/getsentry/sentry-unity/issues/1322 -
                if (Application.platform == RuntimePlatform.Android)
                {
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    var sdkVersion = version.GetStatic<int>("SDK_INT");
                    if (sdkVersion >= 32)
                    {
                        // Unity's built-in fallback
                        return "<unknown>";
                    }
                }
                return SystemInfo.deviceName;
            }
        }

        public Lazy<string> DeviceUniqueIdentifier => new(() => SystemInfo.deviceUniqueIdentifier);
        public Lazy<string> DeviceModel => new(() => SystemInfo.deviceModel);
        /// <summary>
        /// System memory size in megabytes.
        /// </summary>
        public int? SystemMemorySize => SystemInfo.systemMemorySize;
        public int? GraphicsDeviceId => SystemInfo.graphicsDeviceID;
        public string? GraphicsDeviceName => SystemInfo.graphicsDeviceName;
        public Lazy<string>? GraphicsDeviceVendorId => new(() => SystemInfo.graphicsDeviceVendorID.ToString());
        public string? GraphicsDeviceVendor => SystemInfo.graphicsDeviceVendor;
        public int? GraphicsMemorySize => SystemInfo.graphicsMemorySize;
        public Lazy<bool>? GraphicsMultiThreaded => new(() => SystemInfo.graphicsMultiThreaded);
        public string? NpotSupport => SystemInfo.npotSupport.ToString();
        public string? GraphicsDeviceVersion => SystemInfo.graphicsDeviceVersion;
        public string? GraphicsDeviceType => SystemInfo.graphicsDeviceType.ToString();
        public int? MaxTextureSize => SystemInfo.maxTextureSize;
        public bool? SupportsDrawCallInstancing => SystemInfo.supportsInstancing;
        public bool? SupportsRayTracing => SystemInfo.supportsRayTracing;
        public bool? SupportsComputeShaders => SystemInfo.supportsComputeShaders;
        public bool? SupportsGeometryShaders => SystemInfo.supportsGeometryShaders;
        public int? GraphicsShaderLevel => SystemInfo.graphicsShaderLevel;
        public bool? GraphicsUVStartsAtTop => SystemInfo.graphicsUVStartsAtTop;
        public Lazy<bool> IsDebugBuild => new(() => Debug.isDebugBuild);
        public string? EditorVersion => Application.unityVersion;
        public string? InstallMode => Application.installMode.ToString();
        public Lazy<string> TargetFrameRate => new(() => Application.targetFrameRate.ToString());
        public Lazy<string> CopyTextureSupport => new(() => SystemInfo.copyTextureSupport.ToString());
        public Lazy<string> RenderingThreadingMode => new(() => SystemInfo.renderingThreadingMode.ToString());
        public Lazy<DateTimeOffset>? StartTime => new(() => DateTimeOffset.UtcNow.AddSeconds(-Time.realtimeSinceStartup));
    }
}
