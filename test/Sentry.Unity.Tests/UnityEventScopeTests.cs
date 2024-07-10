using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using Object = UnityEngine.Object;

// TODO do we need a real (working) DSN in these tests?
namespace Sentry.Unity.Tests;

public sealed class UnityEventProcessorThreadingTests
{
    private GameObject _gameObject = null!;
    private TestLogger _testLogger = null!;
    private SentryMonoBehaviour _sentryMonoBehaviour = null!;
    private TestApplication _testApplication = null!;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("ScopeTest");
        _testLogger = new TestLogger();
        _sentryMonoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
        _testApplication = new();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_gameObject);

        if (SentrySdk.IsEnabled)
        {
            SentryUnity.Close();
        }
    }

    public string FormatLogs(List<(SentryLevel, string, Exception?)> logs)
    {
        var sb = new StringBuilder()
            .AppendLine("Logs found:");
        int counter = 1;
        foreach (var log in logs)
        {
            sb = sb.AppendLine($"[{counter}] - Level: {log.Item1} - Message: {log.Item2} - Exception: {log.Item3}");
            counter++;
        }
        return sb.AppendLine(" === END ===").ToString();
    }

    [Test]
    public void SentrySdkCaptureEvent_OnNotUIThread_Succeeds()
    {
        // arrange
        var options = new SentryUnityOptions
        {
            Dsn = "https://a520c186ed684a8aa7d5d334bd7dab52@o447951.ingest.sentry.io/5801250",
            Enabled = true,
            AttachStacktrace = true,
            Debug = true,
            DiagnosticLogger = _testLogger
        };
        SentryUnity.Init(options);

        var sentryEvent = new SentryEvent
        {
            Message = new SentryMessage
            {
                Message = NUnit.Framework.TestContext.CurrentContext.Test.Name
            }
        };

        // act
        Task.Run(() => SentrySdk.CaptureEvent(sentryEvent)).Wait();

        SentrySdk.FlushAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();

        // assert
        var logsFound = _testLogger.Logs.Where(log => log.logLevel >= SentryLevel.Warning && log.message != "Cache directory is empty.").ToList();

        Assert.Zero(logsFound.Count, FormatLogs(logsFound));

        // Sanity check: At least some logs must have been printed
        Assert.NotZero(_testLogger.Logs.Count(log => log.logLevel <= SentryLevel.Info));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SentrySdkCaptureEvent(bool collectOnUiThread)
    {
        // arrange
        var sysInfo = _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
        {
            GraphicsDeviceVendorId = new Lazy<string>(() => "VendorId"),
            GraphicsMultiThreaded = new Lazy<bool>(() => true),
            DeviceType = new Lazy<string>(() => "Android"),
            DeviceModel = new Lazy<string>(() => "DeviceModel"),
            DeviceUniqueIdentifier = new Lazy<string>(() => "83fdd6d4-50b1-4735-a4d1-d4f7de64aff0"),
            IsDebugBuild = new Lazy<bool>(() => true),
            TargetFrameRate = new Lazy<string>(() => "-1"),
            CopyTextureSupport = new Lazy<string>(() => "Basic, Copy3D, DifferentTypes, TextureToRT, RTToTexture"),
            RenderingThreadingMode = new Lazy<string>(() => "MultiThreaded"),
            StartTime = new(() => DateTimeOffset.UtcNow),
        };
        var options = new SentryUnityOptions(_sentryMonoBehaviour, _testApplication, false)
        {
            Dsn = "https://b8fd848b31444e80aa102e96d2a6a648@o510466.ingest.sentry.io/5606182",
            Enabled = true,
            AttachStacktrace = true,
            SendDefaultPii = true, // for Device.DeviceUniqueIdentifier
            Debug = true,
            DiagnosticLogger = _testLogger
        };

        if (collectOnUiThread)
        {
            _sentryMonoBehaviour.CollectData();
        }
        else
        {
            // Note: Task.Run().Wait() may be executed on the main thread if it hasn't yet started when Wait() runs.
            // We prevent it by explicitly sleeping on the main thread
            var task = Task.Run(_sentryMonoBehaviour.CollectData);
            Thread.Sleep(10);
            task.Wait();
        }

        SentryUnity.Init(options);

        // act & assert
        for (int i = 0; i <= 1; i++)
        {
            var @event = new SentryEvent()
            {
                Message = NUnit.Framework.TestContext.CurrentContext.Test.Name
            };

            // Events should have the same context, regardless of the thread they were issued on.
            // The context only depends on the thread the data has been collected in, see CollectData() above.
            if (i == 0)
            {
                var task = Task.Run(() => SentrySdk.CaptureEvent(@event));
                Thread.Sleep(10);
                task.Wait();
            }
            else
            {
                SentrySdk.CaptureEvent(@event);
            }
            SentrySdk.FlushAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();

            if (collectOnUiThread)
            {
                Assert.AreEqual(sysInfo.GraphicsDeviceVendorId!.Value, @event.Contexts.Gpu.VendorId);
                Assert.AreEqual(sysInfo.GraphicsMultiThreaded!.Value, @event.Contexts.Gpu.MultiThreadedRendering);
                Assert.AreEqual(sysInfo.DeviceType!.Value, @event.Contexts.Device.DeviceType);
                Assert.AreEqual(sysInfo.DeviceModel!.Value, @event.Contexts.Device.Model);
                Assert.AreEqual(sysInfo.DeviceUniqueIdentifier!.Value, @event.Contexts.Device.DeviceUniqueIdentifier);
                Assert.AreEqual(sysInfo.IsDebugBuild!.Value ? "debug" : "release", @event.Contexts.App.BuildType);

                @event.Contexts.TryGetValue(Unity.Protocol.Unity.Type, out var unityProtocolObject);
                var unityContext = unityProtocolObject as Unity.Protocol.Unity;
                Assert.IsNotNull(unityContext);
                Assert.AreEqual(sysInfo.TargetFrameRate!.Value, unityContext!.TargetFrameRate);
                Assert.AreEqual(sysInfo.CopyTextureSupport!.Value, unityContext.CopyTextureSupport);
                Assert.AreEqual(sysInfo.RenderingThreadingMode!.Value, unityContext.RenderingThreadingMode);
            }
            else
            {
                Assert.IsNull(@event.Contexts.Gpu.VendorId);
                Assert.IsNull(@event.Contexts.Gpu.MultiThreadedRendering);
                Assert.IsNull(@event.Contexts.Device.DeviceType);
                Assert.IsNull(@event.Contexts.Device.Model);
                Assert.IsNull(@event.Contexts.Device.DeviceUniqueIdentifier);
                Assert.IsNull(@event.Contexts.App.BuildType);
                // TODO Assert.IsNull( @event.Contexts.App.StartTime);


                Unity.Protocol.Unity? unityContext;
                if (!@event.Contexts.TryGetValue(Unity.Protocol.Unity.Type, out var contextValue) || (unityContext = contextValue as Unity.Protocol.Unity) == null)
                {
                    unityContext = new Unity.Protocol.Unity();
                    @event.Contexts[Unity.Protocol.Unity.Type] = unityContext;
                }

                Assert.IsNull(unityContext.TargetFrameRate);
                Assert.IsNull(unityContext.CopyTextureSupport);
                Assert.IsNull(unityContext.RenderingThreadingMode);
            }
            Assert.IsNull(@event.ServerName);
        }
    }
}

public sealed class UnityEventProcessorTests
{
    private GameObject _gameObject = null!;
    private SentryMonoBehaviour _sentryMonoBehaviour = null!;
    private MainThreadData _mainThreadData => _sentryMonoBehaviour.MainThreadData;
    private SentryUnityOptions _sentryOptions = null!;
    private TestApplication _testApplication = null!;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("ProcessorTest");
        _sentryMonoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
        _sentryOptions = new SentryUnityOptions
        {
            Debug = true,
            DiagnosticLogger = new TestLogger()
        };
        _testApplication = new();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_gameObject);
    }

    [Test]
    public void SdkInfo_Correct()
    {
        // arrange
        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        sut.ConfigureScope(scope);

        // assert
        Assert.AreEqual(UnitySdkInfo.Name, scope.Sdk.Name);
        Assert.AreEqual(UnitySdkInfo.Version, scope.Sdk.Version);

        var package = scope.Sdk.Packages.FirstOrDefault();
        Assert.IsNotNull(package);
        Assert.AreEqual(UnitySdkInfo.PackageName, package!.Name);
        Assert.AreEqual(UnitySdkInfo.Version, package!.Version);
    }

    [TestCaseSource(nameof(EditorSimulatorValues))]
    public void EventDeviceSimulator_SetCorrectly(bool isEditor, bool? isSimulator)
    {
        // arrange
        var testApplication = new TestApplication(isEditor);
        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        sut.ConfigureScope(scope);

        // assert
        Assert.AreEqual(scope.Contexts.Device.Simulator, isSimulator);
    }

    private static readonly object[] EditorSimulatorValues =
    {
        new object[] { true, true },
        new object[] { false, null! }
    };

    [Test]
    public void DeviceUniqueIdentifierWithSendDefaultPii_IsNotNull()
    {
        // arrange
        var sentryOptions = new SentryUnityOptions { SendDefaultPii = true };
        var sut = new UnityScopeUpdater(sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // act

        // assert
        Assert.IsNotNull(scope.Contexts.Device.DeviceUniqueIdentifier);
    }

    [Test]
    public void AppProtocol_Assigned()
    {
        // arrange
        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        Assert.IsNotNull(scope.Contexts.App.StartTime);
        Assert.IsNotNull(scope.Contexts.App.BuildType);
    }

    [Test]
    public void UserId_SetIfEmpty()
    {
        // arrange
        var options = new SentryUnityOptions { DefaultUserId = "foo" };
        var sut = new UnityScopeUpdater(options, _mainThreadData, _testApplication);
        var scope = new Scope(options);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        Assert.AreEqual(scope.User.Id, options.DefaultUserId);
    }

    [Test]
    public void UserId_UnchangedIfNonEmpty()
    {
        // arrange
        var options = new SentryUnityOptions { DefaultUserId = "foo" };
        var sut = new UnityScopeUpdater(options, _mainThreadData, _testApplication);
        var scope = new Scope(options);
        scope.User.Id = "bar";

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        Assert.AreEqual(scope.User.Id, "bar");
    }

    [Test]
    public void Tags_Set()
    {
        // arrange
        _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
        {
            SupportsDrawCallInstancing = true,
            DeviceType = new(() => "test type"),
            DeviceUniqueIdentifier = new(() => "f810306c-68db-4ebe-89ba-13c457449339"),
            InstallMode = ApplicationInstallMode.Store.ToString()
        };

        var sentryOptions = new SentryUnityOptions { SendDefaultPii = true };
        var scopeUpdater = new UnityScopeUpdater(sentryOptions, _mainThreadData, _testApplication);
        var unityEventProcessor = new UnityEventProcessor(sentryOptions, _sentryMonoBehaviour);
        var scope = new Scope(sentryOptions);
        var sentryEvent = new SentryEvent();
        var transaction = new SentryTransaction("name", "operation");

        // act
        _sentryMonoBehaviour.CollectData();
        scopeUpdater.ConfigureScope(scope);
        scope.Apply(sentryEvent);
        scope.Apply(transaction);
        unityEventProcessor.Process(sentryEvent);
        unityEventProcessor.Process(transaction);

        // assert
        AssertEventProcessorTags(sentryEvent.Tags);
        AssertEventProcessorTags(transaction.Tags);
    }

    private void AssertEventProcessorTags(IReadOnlyDictionary<string, string> tags)
    {
        Assert.IsNotNull(tags);
        Assert.NotZero(tags.Count);

        var unityInstallMode = tags.SingleOrDefault(t => t.Key == "unity.install_mode");
        Assert.NotNull(unityInstallMode);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.InstallMode, unityInstallMode.Value);

        var supportsInstancing = tags.SingleOrDefault(t => t.Key == "unity.gpu.supports_instancing");
        Assert.NotNull(supportsInstancing);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsDrawCallInstancing, bool.Parse(supportsInstancing.Value));

        var deviceType = tags.SingleOrDefault(t => t.Key == "unity.device.device_type");
        Assert.NotNull(deviceType);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceType!.Value, deviceType.Value);

        var deviceUniqueIdentifier = tags.SingleOrDefault(t => t.Key == "unity.device.unique_identifier");
        Assert.NotNull(deviceUniqueIdentifier);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceUniqueIdentifier!.Value, deviceUniqueIdentifier.Value);

        var isMainThread = tags.SingleOrDefault(t => t.Key == "unity.is_main_thread");
        Assert.NotNull(isMainThread);
        Assert.AreEqual("true", isMainThread.Value);
    }

    [Test]
    public void OperatingSystemProtocol_Assigned()
    {
        // arrange
        _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo { OperatingSystem = "Windows" };
        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.OperatingSystem, scope.Contexts.OperatingSystem.RawDescription);
    }

    [Test]
    public void DeviceProtocol_Assigned()
    {
        const long toByte = 1048576L; // in `UnityEventProcessor.PopulateDevice`
        _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
        {
            ProcessorCount = 1,
            DeviceType = new Lazy<string>(() => "Console"),
            CpuDescription = "Intel(R) Core(TM)2 Quad CPU Q6600 @ 2.40GHz",
            SupportsVibration = true,
            DeviceName = "hostname",
            DeviceModel = new Lazy<string>(() => "Samsung Galaxy S3"),
            SystemMemorySize = 16000
        };
        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        var device = scope.Contexts.Device;
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.ProcessorCount, device.ProcessorCount);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceType!.Value, device.DeviceType);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.CpuDescription, device.CpuDescription);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsVibration, device.SupportsVibration);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceName, device.Name);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceModel!.Value, device.Model);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SystemMemorySize * toByte, device.MemorySize);
    }

    [Test]
    public void UnityProtocol_Assigned()
    {
        _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
        {
            EditorVersion = "TestEditorVersion2022.3.2f1",
            InstallMode = "Editor",
            TargetFrameRate = new Lazy<string>(() => "-1"),
            CopyTextureSupport = new Lazy<string>(() => "Basic, Copy3D, DifferentTypes, TextureToRT, RTToTexture"),
            RenderingThreadingMode = new Lazy<string>(() => "MultiThreaded")
        };
        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        scope.Contexts.TryGetValue(Unity.Protocol.Unity.Type, out var unityProtocolObject);
        var unityProtocol = unityProtocolObject as Unity.Protocol.Unity;
        Assert.IsNotNull(unityProtocol);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.EditorVersion, unityProtocol!.EditorVersion);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.InstallMode, unityProtocol.InstallMode);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.TargetFrameRate!.Value, unityProtocol.TargetFrameRate);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.CopyTextureSupport!.Value, unityProtocol.CopyTextureSupport);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.RenderingThreadingMode!.Value, unityProtocol.RenderingThreadingMode);
    }

    [Test]
    public void GpuProtocol_Assigned()
    {
        _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
        {
            GraphicsDeviceId = 1,
            GraphicsDeviceName = "GeForce RTX 3090",
            GraphicsDeviceVendorId = new Lazy<string>(() => "25"),
            GraphicsDeviceVendor = "NVIDIA",
            GraphicsMemorySize = 24000,
            GraphicsMultiThreaded = new Lazy<bool>(() => true),
            NpotSupport = "true",
            GraphicsDeviceVersion = "version212134",
            GraphicsDeviceType = "devicetype",
            MaxTextureSize = 1680,
            SupportsDrawCallInstancing = true,
            SupportsRayTracing = true,
            SupportsComputeShaders = true,
            SupportsGeometryShaders = true
        };
        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        var gpu = scope.Contexts.Gpu;
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceId, gpu.Id);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceName, gpu.Name);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVendorId!.Value, gpu.VendorId);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVendor, gpu.VendorName);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsMemorySize, gpu.MemorySize);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsMultiThreaded!.Value, gpu.MultiThreadedRendering);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.NpotSupport, gpu.NpotSupport);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVersion, gpu.Version);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceType, gpu.ApiType);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.MaxTextureSize, gpu.MaxTextureSize);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsDrawCallInstancing, gpu.SupportsDrawCallInstancing);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsRayTracing, gpu.SupportsRayTracing);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsComputeShaders, gpu.SupportsComputeShaders);
        Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsGeometryShaders, gpu.SupportsGeometryShaders);
    }

    [Test]
    public void GpuProtocolGraphicsShaderLevel_Assigned(
        [ValueSource(nameof(ShaderLevels))] (int, string) shaderValue)
    {
        var (shaderLevel, shaderDescription) = shaderValue;

        _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
        {
            GraphicsShaderLevel = shaderLevel
        };

        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        Assert.AreEqual(shaderDescription, scope.Contexts.Gpu.GraphicsShaderLevel);
    }

    private static readonly (int shaderLevel, string shaderDescription)[] ShaderLevels =
    {
        (20, "Shader Model 2.0"),
        (25, "Shader Model 2.5"),
        (30, "Shader Model 3.0"),
        (35, "OpenGL ES 3.0"),
        (40, "Shader Model 4.0"),
        (45, "Metal / OpenGL ES 3.1"),
        (46, "OpenGL 4.1"),
        (50, "Shader Model 5.0"),
        (21, "21")
    };

    [Test]
    public void GpuProtocolGraphicsShaderLevelMinusOne_Ignored()
    {
        _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
        {
            GraphicsShaderLevel = -1
        };

        var sut = new UnityScopeUpdater(_sentryOptions, _mainThreadData, _testApplication);
        var scope = new Scope(_sentryOptions);

        // act
        _sentryMonoBehaviour.CollectData();
        sut.ConfigureScope(scope);

        // assert
        Assert.IsNull(scope.Contexts.Gpu.GraphicsShaderLevel);
    }
}

internal sealed class TestSentrySystemInfo : ISentrySystemInfo
{
    public int? MainThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId;
    public string? OperatingSystem { get; set; }
    public int? ProcessorCount { get; set; }
    public bool? SupportsVibration { get; set; }
    public Lazy<string>? DeviceType { get; set; }
    public string? CpuDescription { get; set; }
    public string? DeviceName { get; set; }
    public Lazy<string>? DeviceUniqueIdentifier { get; set; }
    public Lazy<string>? DeviceModel { get; set; }
    public int? SystemMemorySize { get; set; }
    public int? GraphicsDeviceId { get; set; }
    public string? GraphicsDeviceName { get; set; }
    public Lazy<string>? GraphicsDeviceVendorId { get; set; }
    public string? GraphicsDeviceVendor { get; set; }
    public int? GraphicsMemorySize { get; set; }
    public Lazy<bool>? GraphicsMultiThreaded { get; set; }
    public string? NpotSupport { get; set; }
    public string? GraphicsDeviceVersion { get; set; }
    public string? GraphicsDeviceType { get; set; }
    public int? MaxTextureSize { get; set; }
    public bool? SupportsDrawCallInstancing { get; set; }
    public bool? SupportsRayTracing { get; set; }
    public bool? SupportsComputeShaders { get; set; }
    public bool? SupportsGeometryShaders { get; set; }
    public int? GraphicsShaderLevel { get; set; }
    public bool? GraphicsUVStartsAtTop { get; }
    public Lazy<bool>? IsDebugBuild { get; set; }
    public string? EditorVersion { get; set; }
    public string? InstallMode { get; set; }
    public Lazy<string>? TargetFrameRate { get; set; }
    public Lazy<string>? CopyTextureSupport { get; set; }
    public Lazy<string>? RenderingThreadingMode { get; set; }
    public Lazy<DateTimeOffset>? StartTime { get; set; }
}