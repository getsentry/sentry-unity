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
    private readonly ISceneManager _sceneManager;

    public UnityScopeUpdater(SentryUnityOptions options, IApplication application, ISceneManager? sceneManager = null)
    {
        _options = options;
        _application = application;
        _sceneManager = sceneManager ?? SceneManagerAdapter.Instance;
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
        app.Name = _application.ProductName;
        app.StartTime = SentryMainThreadData.StartTime;
        var isDebugBuild = SentryMainThreadData.IsDebugBuild;
        app.BuildType = isDebugBuild is null ? null : (isDebugBuild.Value ? "debug" : "release");
    }

    private void PopulateOperatingSystem(OperatingSystem operatingSystem)
    {
        operatingSystem.RawDescription = SentryMainThreadData.OperatingSystem;
    }

    private void PopulateDevice(Device device)
    {
        device.ProcessorCount = SentryMainThreadData.ProcessorCount;
        device.CpuDescription = SentryMainThreadData.CpuDescription;
        device.Timezone = TimeZoneInfo.Local;
        device.SupportsVibration = SentryMainThreadData.SupportsVibration;
        device.Name = SentryMainThreadData.DeviceName;

        // The app can be run in an iOS or Android emulator. We can't safely set a value for simulator.
        device.Simulator = _application.IsEditor ? true : null;
        device.DeviceUniqueIdentifier = _options.SendDefaultPii
            ? SentryMainThreadData.DeviceUniqueIdentifier
            : null;
        device.DeviceType = SentryMainThreadData.DeviceType;
        device.Model = SentryMainThreadData.DeviceModel;

        // This is the approximate amount of system memory in megabytes.
        // This function is not supported on Windows Store Apps and will always return 0.
        if (SentryMainThreadData.SystemMemorySize > 0)
        {
            device.MemorySize = SentryMainThreadData.SystemMemorySize * 1048576L; // Sentry device mem is in Bytes
        }
    }

    private void PopulateGpu(Gpu gpu)
    {
        gpu.Id = SentryMainThreadData.GraphicsDeviceId;
        gpu.Name = SentryMainThreadData.GraphicsDeviceName;
        gpu.VendorName = SentryMainThreadData.GraphicsDeviceVendor;
        gpu.MemorySize = SentryMainThreadData.GraphicsMemorySize;
        gpu.NpotSupport = SentryMainThreadData.NpotSupport;
        gpu.Version = SentryMainThreadData.GraphicsDeviceVersion;
        gpu.ApiType = SentryMainThreadData.GraphicsDeviceType;
        gpu.MaxTextureSize = SentryMainThreadData.MaxTextureSize;
        gpu.SupportsDrawCallInstancing = SentryMainThreadData.SupportsDrawCallInstancing;
        gpu.SupportsRayTracing = SentryMainThreadData.SupportsRayTracing;
        gpu.SupportsComputeShaders = SentryMainThreadData.SupportsComputeShaders;
        gpu.SupportsGeometryShaders = SentryMainThreadData.SupportsGeometryShaders;
        gpu.VendorId = SentryMainThreadData.GraphicsDeviceVendorId;
        gpu.MultiThreadedRendering = SentryMainThreadData.GraphicsMultiThreaded;
        gpu.GraphicsShaderLevel = SentryMainThreadData.GraphicsShaderLevel switch
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
            _ => SentryMainThreadData.GraphicsShaderLevel.ToString()
        };
    }

    private void PopulateUnity(Protocol.Unity unity)
    {
        unity.EditorVersion = SentryMainThreadData.EditorVersion;
        unity.InstallMode = SentryMainThreadData.InstallMode;
        unity.TargetFrameRate = SentryMainThreadData.TargetFrameRate;
        unity.CopyTextureSupport = SentryMainThreadData.CopyTextureSupport;
        unity.RenderingThreadingMode = SentryMainThreadData.RenderingThreadingMode;
        unity.ActiveSceneName = _sceneManager.GetActiveScene().Name;
    }

    private void PopulateTags(Action<string, string> setTag)
    {
        // TODO revisit which tags we should be adding by default
        if (SentryMainThreadData.InstallMode is { } installMode)
        {
            setTag("unity.install_mode", installMode);
        }

        if (SentryMainThreadData.SupportsDrawCallInstancing.HasValue)
        {
            setTag("unity.gpu.supports_instancing", SentryMainThreadData.SupportsDrawCallInstancing.Value.ToTagValue());
        }

        if (SentryMainThreadData.DeviceType is { } deviceType)
        {
            setTag("unity.device.device_type", deviceType);
        }

        if (_options.SendDefaultPii && SentryMainThreadData.DeviceUniqueIdentifier is { } deviceUniqueIdentifier)
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
