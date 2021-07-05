using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sentry.Unity.Tests
{
    public sealed class UnityEventProcessorTests
    {
        private GameObject _gameObject = null!;
        private SentryMonoBehaviour _sentryMonoBehaviour = null!;
        private SentryOptions _sentryOptions = null!;
        private TestApplication _testApplication = null!;
        private Func<SentryMonoBehaviour> _sentryMonoBehaviourGenerator = null!;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("ProcessorTest");
            _sentryMonoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
            _sentryMonoBehaviourGenerator = () => _sentryMonoBehaviour;
            _sentryOptions = new();
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
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviourGenerator, _testApplication);
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
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviourGenerator, testApplication);
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
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviourGenerator, _testApplication);
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
            var sut = new UnityEventProcessor(sentryOptions, _sentryMonoBehaviourGenerator, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            // assert
            Assert.IsNotNull(sentryEvent.Contexts.Device.DeviceUniqueIdentifier);
        }

        [UnityTest]
        public IEnumerator Process_StartTimeOnMainThread_IsNotNull()
        {
            // arrange
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo
            {
                MainThreadId = 1
            };
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions(), _sentryMonoBehaviourGenerator, _testApplication);
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
                DeviceType = "test type",
                DeviceUniqueIdentifier = "f810306c-68db-4ebe-89ba-13c457449339"
            };

            var sentryOptions = new SentryOptions { SendDefaultPii = true };
            var application = new TestApplication(installMode: ApplicationInstallMode.Store);
            var unityEventProcessor = new UnityEventProcessor(sentryOptions, _sentryMonoBehaviourGenerator, application);
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
            Assert.AreEqual(application.InstallMode.ToString(), unityInstallMode.Value);

            var supportsInstancing = tags.SingleOrDefault(t => t.Key == "unity.gpu.supports_instancing");
            Assert.NotNull(supportsInstancing);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.SupportsDrawCallInstancing, bool.Parse(supportsInstancing.Value));

            var deviceType = tags.SingleOrDefault(t => t.Key == "unity.device.device_type");
            Assert.NotNull(deviceType);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceType, deviceType.Value);

            var deviceUniqueIdentifier = tags.SingleOrDefault(t => t.Key == "unity.device.unique_identifier");
            Assert.NotNull(deviceUniqueIdentifier);
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.DeviceUniqueIdentifier, deviceUniqueIdentifier.Value);
        }

        [UnityTest]
        public IEnumerator Process_OperatingSystemProtocol_Assigned()
        {
            // arrange
            _sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo { OperatingSystem = "Windows" };
            var sut = new UnityEventProcessor(_sentryOptions, _sentryMonoBehaviourGenerator, _testApplication);
            var sentryEvent = new SentryEvent();

            // act
            // SentryInitialization always called
            yield return _sentryMonoBehaviour.CollectData();
            sut.Process(sentryEvent);

            // assert
            Assert.AreEqual(_sentryMonoBehaviour.SentrySystemInfo.OperatingSystem, sentryEvent.Contexts.OperatingSystem.Name);
        }
    }

    internal sealed class TestSentrySystemInfo : ISentrySystemInfo
    {
        public int? MainThreadId { get; set; }
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
    }
}
