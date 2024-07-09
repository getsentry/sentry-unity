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
    private readonly MainThreadData _mainThreadData;
    private readonly IApplication _application;

    public UnityScopeIntegration(SentryMonoBehaviour monoBehaviour, IApplication application)
    {
        _mainThreadData = monoBehaviour.MainThreadData;
        _application = application;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        var scopeUpdater = new UnityScopeUpdater((SentryUnityOptions)options, _mainThreadData, _application);
        hub.ConfigureScope(scopeUpdater.ConfigureScope);
    }
}

internal class UnityScopeUpdater
{
    private readonly SentryUnityOptions _options;
    private readonly MainThreadData _mainThreadData;
    private readonly IApplication _application;

    public UnityScopeUpdater(SentryUnityOptions options, MainThreadData mainThreadData, IApplication application)
    {
        _options = options;
        _mainThreadData = mainThreadData;
        _application = application;
    }

    public void ConfigureScope(Scope scope)
    {
        PopulateSdk(scope.Sdk);
        PopulateApp(scope.Contexts.App);
        PopulateOperatingSystem(scope.Contexts.OperatingSystem);
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
        app.StartTime = _mainThreadData.StartTime;
        var isDebugBuild = _mainThreadData.IsDebugBuild;
        app.BuildType = isDebugBuild is null ? null : (isDebugBuild.Value ? "debug" : "release");
    }

    private void PopulateOperatingSystem(OperatingSystem operatingSystem)
    {
        operatingSystem.RawDescription = _mainThreadData.OperatingSystem;
    }

    private void PopulateDevice(Device device)
    {
        device.ProcessorCount = _mainThreadData.ProcessorCount;
        device.CpuDescription = _mainThreadData.CpuDescription;
        device.Timezone = TimeZoneInfo.Local;
        device.SupportsVibration = _mainThreadData.SupportsVibration;
        device.Name = _mainThreadData.DeviceName;

        // The app can be run in an iOS or Android emulator. We can't safely set a value for simulator.
        device.Simulator = _application.IsEditor ? true : null;
        device.DeviceUniqueIdentifier = _options.SendDefaultPii
            ? _mainThreadData.DeviceUniqueIdentifier
            : null;
        device.DeviceType = _mainThreadData.DeviceType;
        device.Model = _mainThreadData.DeviceModel;

        // This is the approximate amount of system memory in megabytes.
        // This function is not supported on Windows Store Apps and will always return 0.
        if (_mainThreadData.SystemMemorySize > 0)
        {
            device.MemorySize = _mainThreadData.SystemMemorySize * 1048576L; // Sentry device mem is in Bytes
        }
    }

    private void PopulateGpu(Gpu gpu)
    {
        gpu.Id = _mainThreadData.GraphicsDeviceId;
        gpu.Name = _mainThreadData.GraphicsDeviceName;
        gpu.VendorName = _mainThreadData.GraphicsDeviceVendor;
        gpu.MemorySize = _mainThreadData.GraphicsMemorySize;
        gpu.NpotSupport = _mainThreadData.NpotSupport;
        gpu.Version = _mainThreadData.GraphicsDeviceVersion;
        gpu.ApiType = _mainThreadData.GraphicsDeviceType;
        gpu.MaxTextureSize = _mainThreadData.MaxTextureSize;
        gpu.SupportsDrawCallInstancing = _mainThreadData.SupportsDrawCallInstancing;
        gpu.SupportsRayTracing = _mainThreadData.SupportsRayTracing;
        gpu.SupportsComputeShaders = _mainThreadData.SupportsComputeShaders;
        gpu.SupportsGeometryShaders = _mainThreadData.SupportsGeometryShaders;
        gpu.VendorId = _mainThreadData.GraphicsDeviceVendorId;
        gpu.MultiThreadedRendering = _mainThreadData.GraphicsMultiThreaded;
        gpu.GraphicsShaderLevel = _mainThreadData.GraphicsShaderLevel switch
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
            _ => _mainThreadData.GraphicsShaderLevel.ToString()
        };
    }

    private void PopulateUnity(Protocol.Unity unity)
    {
        unity.EditorVersion = _mainThreadData.EditorVersion;
        unity.InstallMode = _mainThreadData.InstallMode;
        unity.TargetFrameRate = _mainThreadData.TargetFrameRate;
        unity.CopyTextureSupport = _mainThreadData.CopyTextureSupport;
        unity.RenderingThreadingMode = _mainThreadData.RenderingThreadingMode;
    }

    private void PopulateTags(Action<string, string> setTag)
    {
        // TODO revisit which tags we should be adding by default
        if (_mainThreadData.InstallMode is { } installMode)
        {
            setTag("unity.install_mode", installMode);
        }

        if (_mainThreadData.SupportsDrawCallInstancing.HasValue)
        {
            setTag("unity.gpu.supports_instancing", _mainThreadData.SupportsDrawCallInstancing.Value.ToTagValue());
        }

        if (_mainThreadData.DeviceType is { } deviceType)
        {
            setTag("unity.device.device_type", deviceType);
        }

        if (_options.SendDefaultPii && _mainThreadData.DeviceUniqueIdentifier is { } deviceUniqueIdentifier)
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