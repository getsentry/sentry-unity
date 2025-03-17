using System;
using Sentry.Protocol;
using Sentry.Reflection;
using Sentry.Integrations;
using Sentry.Unity.Integrations;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Unity;

internal static class UnitySdkInfo
{
    public static string Version { get; } = typeof(UnitySdkInfo).Assembly.GetNameAndVersion().Version ?? "0.0.0";
    public const string Name = "sentry.dotnet.unity";
    public const string PackageName = "upm:sentry.unity";
}

internal class UnityScopeIntegration : ISdkIntegration
{
    private readonly IApplication _application;

    public UnityScopeIntegration(IApplication application)
    {
        _application = application;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        var scopeUpdater = new UnityScopeUpdater((SentryUnityOptions)options, _application);
        hub.ConfigureScope(scopeUpdater.ConfigureScope);
    }
}

internal class UnityScopeUpdater
{
    private readonly SentryUnityOptions _options;
    private readonly IApplication _application;

    public UnityScopeUpdater(SentryUnityOptions options, IApplication application)
    {
        _options = options;
        _application = application;
    }

    public void ConfigureScope(Scope scope)
    {
        PopulateSdk(scope.Sdk);
        PopulateApp(scope.Contexts.App);
        // PopulateOperatingSystem(scope.Contexts.OperatingSystem);
        PopulateDevice(scope.Contexts.Device);
        PopulateGpu(scope.Contexts.Gpu);

        var unity = new Protocol.Unity();
        PopulateUnity(unity);
        scope.Contexts.Add(Protocol.Unity.Type, unity);

        PopulateTags(scope.SetTag);
        PopulateUser(scope);
    }

    private static void PopulateSdk(SdkVersion sdk)
    {
        sdk.AddPackage(UnitySdkInfo.PackageName, UnitySdkInfo.Version);
        sdk.Name = UnitySdkInfo.Name;
        sdk.Version = UnitySdkInfo.Version;
    }

    private void PopulateApp(App app)
    {
        app.StartTime = MainThreadData.StartTime;
        var isDebugBuild = MainThreadData.IsDebugBuild;
        app.BuildType = isDebugBuild is null ? null : (isDebugBuild.Value ? "debug" : "release");
    }

    private void PopulateOperatingSystem(OperatingSystem operatingSystem)
    {
        operatingSystem.RawDescription = MainThreadData.OperatingSystem;
    }

    private void PopulateDevice(Device device)
    {
        device.ProcessorCount = MainThreadData.ProcessorCount;
        device.CpuDescription = MainThreadData.CpuDescription;
        device.Timezone = TimeZoneInfo.Local;
        device.SupportsVibration = MainThreadData.SupportsVibration;
        device.Name = MainThreadData.DeviceName;

        // The app can be run in an iOS or Android emulator. We can't safely set a value for simulator.
        device.Simulator = _application.IsEditor ? true : null;
        device.DeviceUniqueIdentifier = _options.SendDefaultPii
            ? MainThreadData.DeviceUniqueIdentifier
            : null;
        device.DeviceType = MainThreadData.DeviceType;
        device.Model = MainThreadData.DeviceModel;

        // This is the approximate amount of system memory in megabytes.
        // This function is not supported on Windows Store Apps and will always return 0.
        if (MainThreadData.SystemMemorySize > 0)
        {
            device.MemorySize = MainThreadData.SystemMemorySize * 1048576L; // Sentry device mem is in Bytes
        }
    }

    private void PopulateGpu(Gpu gpu)
    {
        gpu.Id = MainThreadData.GraphicsDeviceId;
        gpu.Name = MainThreadData.GraphicsDeviceName;
        gpu.VendorName = MainThreadData.GraphicsDeviceVendor;
        gpu.MemorySize = MainThreadData.GraphicsMemorySize;
        gpu.NpotSupport = MainThreadData.NpotSupport;
        gpu.Version = MainThreadData.GraphicsDeviceVersion;
        gpu.ApiType = MainThreadData.GraphicsDeviceType;
        gpu.MaxTextureSize = MainThreadData.MaxTextureSize;
        gpu.SupportsDrawCallInstancing = MainThreadData.SupportsDrawCallInstancing;
        gpu.SupportsRayTracing = MainThreadData.SupportsRayTracing;
        gpu.SupportsComputeShaders = MainThreadData.SupportsComputeShaders;
        gpu.SupportsGeometryShaders = MainThreadData.SupportsGeometryShaders;
        gpu.VendorId = MainThreadData.GraphicsDeviceVendorId;
        gpu.MultiThreadedRendering = MainThreadData.GraphicsMultiThreaded;
        gpu.GraphicsShaderLevel = MainThreadData.GraphicsShaderLevel switch
        {
            null => null,
            -1 => null,
            20 => "Shader Model 2.0",
            25 => "Shader Model 2.5",
            30 => "Shader Model 3.0",
            35 => "OpenGL ES 3.0",
            40 => "Shader Model 4.0",
            45 => "Metal / OpenGL ES 3.1",
            46 => "OpenGL 4.1",
            50 => "Shader Model 5.0",
            _ => MainThreadData.GraphicsShaderLevel.ToString()
        };
    }

    private void PopulateUnity(Protocol.Unity unity)
    {
        unity.EditorVersion = MainThreadData.EditorVersion;
        unity.InstallMode = MainThreadData.InstallMode;
        unity.TargetFrameRate = MainThreadData.TargetFrameRate;
        unity.CopyTextureSupport = MainThreadData.CopyTextureSupport;
        unity.RenderingThreadingMode = MainThreadData.RenderingThreadingMode;
    }

    private void PopulateTags(Action<string, string> setTag)
    {
        // TODO revisit which tags we should be adding by default
        if (MainThreadData.InstallMode is { } installMode)
        {
            setTag("unity.install_mode", installMode);
        }

        if (MainThreadData.SupportsDrawCallInstancing.HasValue)
        {
            setTag("unity.gpu.supports_instancing", MainThreadData.SupportsDrawCallInstancing.Value.ToTagValue());
        }

        if (MainThreadData.DeviceType is { } deviceType)
        {
            setTag("unity.device.device_type", deviceType);
        }

        if (_options.SendDefaultPii && MainThreadData.DeviceUniqueIdentifier is { } deviceUniqueIdentifier)
        {
            setTag("unity.device.unique_identifier", deviceUniqueIdentifier);
        }
    }

    private void PopulateUser(Scope scope)
    {
        if (_options.DefaultUserId is not null)
        {
            if (scope.User.Id is null)
            {
                scope.User.Id = _options.DefaultUserId;
            }
        }
    }
}
