using System;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public sealed class ContextWriterTests
{
    private GameObject _gameObject = null!;
    private SentryMonoBehaviour _sentryMonoBehaviour = null!;
    private TestApplication _testApplication = null!;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("ContextWriterTest");
        _sentryMonoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
        _testApplication = new();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(_gameObject);
    }

    [Test]
    public void Arguments()
    {
        // arrange
        var sysInfo = new TestSentrySystemInfo
        {
            OperatingSystem = "OperatingSystem",
            ProcessorCount = 1,
            SupportsVibration = true,
            DeviceType = new(() => "DeviceType"),
            CpuDescription = "CpuDescription",
            DeviceName = "DeviceName",
            DeviceUniqueIdentifier = new Lazy<string>(() => "DeviceUniqueIdentifier"),
            DeviceModel = new(() => "DeviceModel"),
            SystemMemorySize = 2,
            GraphicsDeviceId = 3,
            GraphicsDeviceName = "GraphicsDeviceName",
            GraphicsDeviceVendorId = new(() => "GraphicsDeviceVendorId"),
            GraphicsDeviceVendor = "GraphicsDeviceVendor",
            GraphicsMemorySize = 4,
            GraphicsMultiThreaded = new(() => true),
            NpotSupport = "NpotSupport",
            GraphicsDeviceVersion = "GraphicsDeviceVersion",
            GraphicsDeviceType = "GraphicsDeviceType",
            MaxTextureSize = 5,
            SupportsDrawCallInstancing = true,
            SupportsRayTracing = false,
            SupportsComputeShaders = true,
            SupportsGeometryShaders = false,
            GraphicsShaderLevel = 6,
            IsDebugBuild = new(() => true),
            InstallMode = "InstallMode",
            TargetFrameRate = new(() => "TargetFrameRate"),
            CopyTextureSupport = new(() => "CopyTextureSupport"),
            RenderingThreadingMode = new(() => "RenderingThreadingMode"),
            StartTime = new(() => DateTimeOffset.UtcNow),

        };
        var context = new MockContextWriter();
        var options = new SentryUnityOptions(_sentryMonoBehaviour, _testApplication, false)
        {
            Dsn = "http://publickey@localhost/12345",
            Enabled = true,
            AttachStacktrace = true,
            SendDefaultPii = true,
            Debug = true,
            DiagnosticLogger = new TestLogger(),
            NativeContextWriter = context,
        };

        // In an actual build, it's getting collected before initialization via the RuntimeInitializeOnLoadMethod
        SentryMainThreadData.SentrySystemInfo = sysInfo;
        SentryMainThreadData.Collect();

        // act
        SentryUnity.Init(options);

        // assert
        Assert.IsTrue(context.SyncFinished.WaitOne(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(sysInfo.StartTime?.Value.ToString("o"), context.AppStartTime);
        Assert.AreEqual("debug", context.AppBuildType);
        Assert.AreEqual(sysInfo.OperatingSystem, context.OperatingSystemRawDescription);
        Assert.AreEqual(sysInfo.ProcessorCount, context.DeviceProcessorCount);
        Assert.AreEqual(sysInfo.CpuDescription, context.DeviceCpuDescription);
        Assert.AreEqual(TimeZoneInfo.Local.Id, context.DeviceTimezone);
        Assert.AreEqual(sysInfo.SupportsVibration, context.DeviceSupportsVibration);
        Assert.AreEqual(sysInfo.DeviceName, context.DeviceName);
        Assert.AreEqual(_testApplication.IsEditor, context.DeviceSimulator);
        Assert.AreEqual(sysInfo.DeviceUniqueIdentifier?.Value, context.DeviceDeviceUniqueIdentifier);
        Assert.AreEqual(sysInfo.DeviceType?.Value, context.DeviceDeviceType);
        Assert.AreEqual(sysInfo.DeviceModel?.Value, context.DeviceModel);
        Assert.AreEqual(sysInfo.SystemMemorySize * 1048576L, context.DeviceMemorySize);
        Assert.AreEqual(sysInfo.GraphicsDeviceId, context.GpuId);
        Assert.AreEqual(sysInfo.GraphicsDeviceName, context.GpuName);
        Assert.AreEqual(sysInfo.GraphicsDeviceVendor, context.GpuVendorName);
        Assert.AreEqual(sysInfo.GraphicsMemorySize, context.GpuMemorySize);
        Assert.AreEqual(sysInfo.NpotSupport, context.GpuNpotSupport);
        Assert.AreEqual(sysInfo.GraphicsDeviceVersion, context.GpuVersion);
        Assert.AreEqual(sysInfo.GraphicsDeviceType, context.GpuApiType);
        Assert.AreEqual(sysInfo.MaxTextureSize, context.GpuMaxTextureSize);
        Assert.AreEqual(sysInfo.SupportsDrawCallInstancing, context.GpuSupportsDrawCallInstancing);
        Assert.AreEqual(sysInfo.SupportsRayTracing, context.GpuSupportsRayTracing);
        Assert.AreEqual(sysInfo.SupportsComputeShaders, context.GpuSupportsComputeShaders);
        Assert.AreEqual(sysInfo.SupportsGeometryShaders, context.GpuSupportsGeometryShaders);
        Assert.AreEqual(sysInfo.GraphicsDeviceVendorId?.Value, context.GpuVendorId);
        Assert.AreEqual(sysInfo.GraphicsMultiThreaded?.Value, context.GpuMultiThreadedRendering);
        Assert.AreEqual(sysInfo.GraphicsShaderLevel.ToString(), context.GpuGraphicsShaderLevel);
        Assert.AreEqual(sysInfo.EditorVersion, context.UnityEditorVersion);
        Assert.AreEqual(sysInfo.InstallMode, context.UnityInstallMode);
        Assert.AreEqual(sysInfo.TargetFrameRate?.Value, context.UnityTargetFrameRate);
        Assert.AreEqual(sysInfo.CopyTextureSupport?.Value, context.UnityCopyTextureSupport);
        Assert.AreEqual(sysInfo.RenderingThreadingMode?.Value, context.UnityRenderingThreadingMode);
    }
}

internal sealed class MockContextWriter : ContextWriter
{
    public AutoResetEvent SyncFinished = new AutoResetEvent(false);

    public string? AppStartTime = null;
    public string? AppBuildType = null;
    public string? OperatingSystemRawDescription = null;
    public int? DeviceProcessorCount = null;
    public string? DeviceCpuDescription = null;
    public string? DeviceTimezone = null;
    public bool? DeviceSupportsVibration = null;
    public string? DeviceName = null;
    public bool? DeviceSimulator = null;
    public string? DeviceDeviceUniqueIdentifier = null;
    public string? DeviceDeviceType = null;
    public string? DeviceModel = null;
    public long? DeviceMemorySize = null;
    public int? GpuId = null;
    public string? GpuName = null;
    public string? GpuVendorName = null;
    public int? GpuMemorySize = null;
    public string? GpuNpotSupport = null;
    public string? GpuVersion = null;
    public string? GpuApiType = null;
    public int? GpuMaxTextureSize = null;
    public bool? GpuSupportsDrawCallInstancing = null;
    public bool? GpuSupportsRayTracing = null;
    public bool? GpuSupportsComputeShaders = null;
    public bool? GpuSupportsGeometryShaders = null;
    public string? GpuVendorId = null;
    public bool? GpuMultiThreadedRendering = null;
    public string? GpuGraphicsShaderLevel = null;
    public string? UnityEditorVersion = null;
    public string? UnityInstallMode = null;
    public string? UnityTargetFrameRate = null;
    public string? UnityCopyTextureSupport = null;
    public string? UnityRenderingThreadingMode = null;

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
        string? UnityEditorVersion,
        string? UnityInstallMode,
        string? UnityTargetFrameRate,
        string? UnityCopyTextureSupport,
        string? UnityRenderingThreadingMode
    )
    {
        this.AppStartTime = AppStartTime;
        this.AppBuildType = AppBuildType;
        this.OperatingSystemRawDescription = OperatingSystemRawDescription;
        this.DeviceProcessorCount = DeviceProcessorCount;
        this.DeviceCpuDescription = DeviceCpuDescription;
        this.DeviceTimezone = DeviceTimezone;
        this.DeviceSupportsVibration = DeviceSupportsVibration;
        this.DeviceName = DeviceName;
        this.DeviceSimulator = DeviceSimulator;
        this.DeviceDeviceUniqueIdentifier = DeviceDeviceUniqueIdentifier;
        this.DeviceDeviceType = DeviceDeviceType;
        this.DeviceModel = DeviceModel;
        this.DeviceMemorySize = DeviceMemorySize;
        this.GpuId = GpuId;
        this.GpuName = GpuName;
        this.GpuVendorName = GpuVendorName;
        this.GpuMemorySize = GpuMemorySize;
        this.GpuNpotSupport = GpuNpotSupport;
        this.GpuVersion = GpuVersion;
        this.GpuApiType = GpuApiType;
        this.GpuMaxTextureSize = GpuMaxTextureSize;
        this.GpuSupportsDrawCallInstancing = GpuSupportsDrawCallInstancing;
        this.GpuSupportsRayTracing = GpuSupportsRayTracing;
        this.GpuSupportsComputeShaders = GpuSupportsComputeShaders;
        this.GpuSupportsGeometryShaders = GpuSupportsGeometryShaders;
        this.GpuVendorId = GpuVendorId;
        this.GpuMultiThreadedRendering = GpuMultiThreadedRendering;
        this.GpuGraphicsShaderLevel = GpuGraphicsShaderLevel;
        this.UnityEditorVersion = UnityEditorVersion;
        this.UnityInstallMode = UnityInstallMode;
        this.UnityTargetFrameRate = UnityTargetFrameRate;
        this.UnityCopyTextureSupport = UnityCopyTextureSupport;
        this.UnityRenderingThreadingMode = UnityRenderingThreadingMode;
        SyncFinished.Set();
    }
}
