using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sentry.Unity.Tests
{
    public sealed class UnityEventProcessorThreadingTests
    {
        private GameObject _gameObject = null!;
        private TestLogger _testLogger = null!;
        private SentryMonoBehaviour _sentryMonoBehaviour = null!;
        private TestApplication _testApplication = null!;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("ProcessorTest");
            _testLogger = new TestLogger();
            _sentryMonoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
            _testApplication = new();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_gameObject);
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
                    Message = "Test message"
                }
            };

            // act
            Task.Run(() => SentrySdk.CaptureEvent(sentryEvent))
                .Wait();

            SentrySdk.FlushAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();

            // assert
            Assert.Zero(_testLogger.Logs.Count(log => log.logLevel >= SentryLevel.Warning));
            // Sanity check: At least some logs must have been printed
            Assert.NotZero(_testLogger.Logs.Count(log => log.logLevel <= SentryLevel.Info));
        }

        [UnityTest]
        public IEnumerator SentrySdkCaptureEvent_OnNotUIThreadThenUIThreadThenNotUIThread_Cached()
        {
            // arrange
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
            {
                GraphicsDeviceVendorId = new Lazy<string>(() => "VendorId"),
                GraphicsMultiThreaded = new Lazy<bool>(() => true),
                DeviceType = new Lazy<string>(() => "Android"),
                DeviceModel = new Lazy<string>(() => "DeviceModel"),
                DeviceUniqueIdentifier = new Lazy<string>(() => "83fdd6d4-50b1-4735-a4d1-d4f7de64aff0"),
                IsDebugBuild = new Lazy<bool>(() => true),
                TargetFrameRate = new Lazy<string>(() => "-1"),
                CopyTextureSupport = new Lazy<string>(() => "Basic, Copy3D, DifferentTypes, TextureToRT, RTToTexture"),
                RenderingThreadingMode = new Lazy<string>(() => "MultiThreaded")
            };
            var options = new SentryUnityOptions
            {
                Dsn = "https://b8fd848b31444e80aa102e96d2a6a648@o510466.ingest.sentry.io/5606182",
                Enabled = true,
                AttachStacktrace = true,
                SendDefaultPii = true, // for Device.DeviceUniqueIdentifier
                Debug = true,
                DiagnosticLogger = _testLogger
            };
            options.AddEventProcessor(new UnityEventProcessor(options, _sentryMonoBehaviour, _testApplication));
            SentryUnity.Init(options);

            yield return _sentryMonoBehaviour.CollectData();

            // act & assert
            var nonUiThreadEventDataNotCached = NonUiThread();
            Assert.IsNull(nonUiThreadEventDataNotCached.Contexts.Gpu.VendorId);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCached.Contexts.Gpu.VendorId)));
            Assert.IsNull(nonUiThreadEventDataNotCached.Contexts.Gpu.MultiThreadedRendering);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCached.Contexts.Gpu.MultiThreadedRendering)));
            Assert.IsNull(nonUiThreadEventDataNotCached.Contexts.Device.DeviceType);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCached.Contexts.Device.DeviceType)));
            Assert.IsNull(nonUiThreadEventDataNotCached.Contexts.Device.Model);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCached.Contexts.Device.Model)));
            Assert.IsNull(nonUiThreadEventDataNotCached.Contexts.Device.DeviceUniqueIdentifier);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCached.Contexts.Device.DeviceUniqueIdentifier)));
            Assert.IsNull(nonUiThreadEventDataNotCached.Contexts.App.BuildType);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCached.Contexts.App.BuildType)));
            var nonUiThreadEventDataNotCachedUnity = (Unity.Protocol.Unity)nonUiThreadEventDataNotCached.Contexts.GetOrAdd(Unity.Protocol.Unity.Type, _ => new Unity.Protocol.Unity());
            Assert.IsNull(nonUiThreadEventDataNotCachedUnity.TargetFrameRate);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCachedUnity.TargetFrameRate)));
            Assert.IsNull(nonUiThreadEventDataNotCachedUnity.CopyTextureSupport);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCachedUnity.CopyTextureSupport)));
            Assert.IsNull(nonUiThreadEventDataNotCachedUnity.RenderingThreadingMode);
            Assert.IsTrue(NonUiThreadDebugMessageExists(nameof(nonUiThreadEventDataNotCachedUnity.RenderingThreadingMode)));

            var uiThreadEventDataCached = UiThread();
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVendorId!.Value, uiThreadEventDataCached.Contexts.Gpu.VendorId);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsMultiThreaded!.Value, uiThreadEventDataCached.Contexts.Gpu.MultiThreadedRendering);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceType!.Value, uiThreadEventDataCached.Contexts.Device.DeviceType);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceModel!.Value, uiThreadEventDataCached.Contexts.Device.Model);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceUniqueIdentifier!.Value, uiThreadEventDataCached.Contexts.Device.DeviceUniqueIdentifier);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.IsDebugBuild!.Value ? "debug" : "release", uiThreadEventDataCached.Contexts.App.BuildType);
            var uiThreadEventDataCachedUnity = (Unity.Protocol.Unity)uiThreadEventDataCached.Contexts.GetOrAdd(Unity.Protocol.Unity.Type, _ => new Unity.Protocol.Unity());
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.TargetFrameRate!.Value, uiThreadEventDataCachedUnity.TargetFrameRate);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.CopyTextureSupport!.Value, uiThreadEventDataCachedUnity.CopyTextureSupport);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.RenderingThreadingMode!.Value, uiThreadEventDataCachedUnity.RenderingThreadingMode);

            var nonUiThreadEventDataCached = NonUiThread();
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVendorId!.Value, nonUiThreadEventDataCached.Contexts.Gpu.VendorId);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsMultiThreaded!.Value, nonUiThreadEventDataCached.Contexts.Gpu.MultiThreadedRendering);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceType!.Value, nonUiThreadEventDataCached.Contexts.Device.DeviceType);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceModel!.Value, nonUiThreadEventDataCached.Contexts.Device.Model);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceUniqueIdentifier!.Value, nonUiThreadEventDataCached.Contexts.Device.DeviceUniqueIdentifier);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.IsDebugBuild!.Value ? "debug" : "release", nonUiThreadEventDataCached.Contexts.App.BuildType);
            var nonUiThreadEventDataCachedUnity = (Unity.Protocol.Unity)nonUiThreadEventDataCached.Contexts.GetOrAdd(Unity.Protocol.Unity.Type, _ => new Unity.Protocol.Unity());
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.TargetFrameRate!.Value, nonUiThreadEventDataCachedUnity.TargetFrameRate);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.CopyTextureSupport!.Value, nonUiThreadEventDataCachedUnity.CopyTextureSupport);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.RenderingThreadingMode!.Value, nonUiThreadEventDataCachedUnity.RenderingThreadingMode);

            bool NonUiThreadDebugMessageExists(string propertyName)
                => _testLogger.Logs.Any(log =>
                     log.logLevel == SentryLevel.Debug &&
                     log.message.Contains(propertyName));

            static SentryEvent NonUiThread()
            {
                var sentryEvent = CreateSentryEvent();
                Task.Run(() => SentrySdk.CaptureEvent(sentryEvent))
                    .Wait();
                SentrySdk.FlushAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
                return sentryEvent;
            }

            static SentryEvent UiThread()
            {
                var sentryEvent = CreateSentryEvent();
                SentrySdk.CaptureEvent(sentryEvent);
                SentrySdk.FlushAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
                return sentryEvent;
            }

            static SentryEvent CreateSentryEvent()
                => new()
                {
                    Message = new SentryMessage
                    {
                        Message = "Test message"
                    }
                };
        }
    }

    public sealed class UnityEventProcessorTests
    {
        private GameObject _gameObject = null!;
        private SentryMonoBehaviour _sentryMonoBehaviour = null!;
        private SentryOptions _sentryOptions = null!;
        private TestApplication _testApplication = null!;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("ProcessorTest");
            _sentryMonoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
            _sentryOptions = new SentryOptions
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
        public void Process_SdkInfo_Correct()
        {
            // arrange
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            sut.Process(sentryEvent);

            // assert
            Assert.AreEqual(UnitySdkInfo.Name, sentryEvent.Sdk.Name);
            Assert.AreEqual(UnitySdkInfo.Version, sentryEvent.Sdk.Version);

            var package = sentryEvent.Sdk.Packages.FirstOrDefault();
            Assert.IsNotNull(package);
            Assert.AreEqual(UnitySdkInfo.PackageName, package!.Name);
            Assert.AreEqual(UnitySdkInfo.Version, package!.Version);
        }

        [TestCaseSource(nameof(EditorSimulatorValues))]
        public void Process_EventDeviceSimulator_SetCorrectly(bool isEditor, bool? isSimulator)
        {
            // arrange
            var testApplication = new TestApplication(isEditor);
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, testApplication);
            var sentryEvent = new SentryEvent();

            // act
            sut.Process(sentryEvent);

            // assert
            Assert.AreEqual(sentryEvent.Contexts.Device.Simulator, isSimulator);
        }

        private static readonly object[] EditorSimulatorValues =
        {
            new object[] { true, true },
            new object[] { false, null! }
        };

        [Test]
        public void Process_ServerName_IsNull()
        {
            // arrange
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            sut.Process(sentryEvent);

            // assert
            Assert.IsNull(sentryEvent.ServerName);
        }

        [UnityTest]
        public IEnumerator Process_DeviceUniqueIdentifierWithSendDefaultPii_IsNotNull()
        {
            // arrange
            var sentryOptions = new SentryOptions { SendDefaultPii = true };
            var sut = new UnityEventProcessor(sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            // assert
            Assert.IsNotNull(sentryEvent.Contexts.Device.DeviceUniqueIdentifier);
        }

        [UnityTest]
        public IEnumerator Process_AppProtocol_Assigned()
        {
            // arrange
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
            {
                MainThreadId = 1
            };
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions(), _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            yield return _sentryMonoBehaviour.CollectData();
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.IsNotNull(sentryEvent.Contexts.App.StartTime);
        }

        [UnityTest]
        public IEnumerator Process_Tags_Set()
        {
            // arrange
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
            {
                SupportsDrawCallInstancing = true,
                DeviceType = new Lazy<string>(() => "test type"),
                DeviceUniqueIdentifier = new Lazy<string>(() => "f810306c-68db-4ebe-89ba-13c457449339"),
                InstallMode = ApplicationInstallMode.Store.ToString()
            };

            var sentryOptions = new SentryOptions { SendDefaultPii = true };
            var unityEventProcessor = new UnityEventProcessor(sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            yield return _sentryMonoBehaviour.CollectData();
            unityEventProcessor.Process(sentryEvent);

            // assert
            var tags = sentryEvent.Tags;

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
        }

        [UnityTest]
        public IEnumerator Process_OperatingSystemProtocol_Assigned()
        {
            // arrange
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo { OperatingSystem = "Windows" };
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            // SentryInitialization always called
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            // assert
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.OperatingSystem, sentryEvent.Contexts.OperatingSystem.RawDescription);
        }

        [UnityTest]
        public IEnumerator Process_DeviceProtocol_Assigned()
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
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.ProcessorCount, sentryEvent.Contexts.Device.ProcessorCount);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceType!.Value, sentryEvent.Contexts.Device.DeviceType);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.CpuDescription, sentryEvent.Contexts.Device.CpuDescription);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsVibration, sentryEvent.Contexts.Device.SupportsVibration);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceName, sentryEvent.Contexts.Device.Name);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceModel!.Value, sentryEvent.Contexts.Device.Model);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SystemMemorySize * toByte, sentryEvent.Contexts.Device.MemorySize);
        }

        [UnityTest]
        public IEnumerator Process_UnityProtocol_Assigned()
        {
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
            {
                InstallMode = "Editor",
                TargetFrameRate = new Lazy<string>(() => "-1"),
                CopyTextureSupport = new Lazy<string>(() => "Basic, Copy3D, DifferentTypes, TextureToRT, RTToTexture"),
                RenderingThreadingMode = new Lazy<string>(() => "MultiThreaded")
            };

            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            var unityProtocol = (Unity.Protocol.Unity)sentryEvent.Contexts.GetOrAdd(Unity.Protocol.Unity.Type, _ => new Unity.Protocol.Unity());
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.InstallMode, unityProtocol.InstallMode);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.TargetFrameRate!.Value, unityProtocol.TargetFrameRate);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.CopyTextureSupport!.Value, unityProtocol.CopyTextureSupport);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.RenderingThreadingMode!.Value, unityProtocol.RenderingThreadingMode);
        }

        [UnityTest]
        public IEnumerator Process_GpuProtocol_Assigned()
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
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            // SentryInitialization always called
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceId, sentryEvent.Contexts.Gpu.Id);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceName, sentryEvent.Contexts.Gpu.Name);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVendorId!.Value, sentryEvent.Contexts.Gpu.VendorId);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVendor, sentryEvent.Contexts.Gpu.VendorName);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsMemorySize, sentryEvent.Contexts.Gpu.MemorySize);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsMultiThreaded!.Value, sentryEvent.Contexts.Gpu.MultiThreadedRendering);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.NpotSupport, sentryEvent.Contexts.Gpu.NpotSupport);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceVersion, sentryEvent.Contexts.Gpu.Version);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.GraphicsDeviceType, sentryEvent.Contexts.Gpu.ApiType);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.MaxTextureSize, sentryEvent.Contexts.Gpu.MaxTextureSize);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsDrawCallInstancing, sentryEvent.Contexts.Gpu.SupportsDrawCallInstancing);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsRayTracing, sentryEvent.Contexts.Gpu.SupportsRayTracing);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsComputeShaders, sentryEvent.Contexts.Gpu.SupportsComputeShaders);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsGeometryShaders, sentryEvent.Contexts.Gpu.SupportsGeometryShaders);
        }

        [UnityTest]
        public IEnumerator Process_GpuProtocolGraphicsShaderLevel_Assigned(
            [ValueSource(nameof(ShaderLevels))] (int, string) shaderValue)
        {
            var (shaderLevel, shaderDescription) = shaderValue;

            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
            {
                GraphicsShaderLevel = shaderLevel
            };

            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            // SentryInitialization always called
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            Assert.AreEqual(shaderDescription, sentryEvent.Contexts.Gpu.GraphicsShaderLevel);
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

        [UnityTest]
        public IEnumerator Process_GpuProtocolGraphicsShaderLevelMinusOne_Ignored()
        {
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
            {
                GraphicsShaderLevel = -1
            };

            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviour, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            // SentryInitialization always called
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            Assert.IsNull(sentryEvent.Contexts.Gpu.GraphicsShaderLevel);
        }
    }

    internal sealed class TestSentrySystemInfo : ISentrySystemInfo
    {
        public int? MainThreadId { get; set; } = 1;
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
        public Lazy<bool>? IsDebugBuild { get; set; }
        public string? InstallMode { get; set; }
        public Lazy<string>? TargetFrameRate { get; set; }
        public Lazy<string>? CopyTextureSupport { get; set; }
        public Lazy<string>? RenderingThreadingMode { get; set; }
    }
}
