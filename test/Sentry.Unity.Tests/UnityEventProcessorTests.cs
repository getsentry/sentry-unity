using System.Collections;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public sealed class UnityEventProcessorTests
    {
        [Test]
        public void Process_SdkInfo_Correct()
        {
            // arrange
            var testApplication = new TestApplication();
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions(), testApplication);
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

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
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions(), testApplication);
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

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
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions());
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.IsNull(sentryEvent.ServerName);
        }

        [Test]
        public void Process_DeviceUniqueIdentifierWithSendDefaultPii_IsNotNull()
        {
            // arrange
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions { SendDefaultPii = true });
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.IsNotNull(sentryEvent.Contexts.Device.DeviceUniqueIdentifier);
        }

        [Test]
        public void Process_StartTimeOnMainThread_IsNotNull()
        {
            // arrange
            var application = new TestApplication(isMainThread: true);
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions(), application);
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.IsNotNull(sentryEvent.Contexts.App.StartTime);
        }

        [Test]
        public void Process_Tags_Set()
        {
            // arrange
            var unityEventProcessor = new UnityEventProcessor(new SentryOptions { SendDefaultPii = true });
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

            // assert
            var tags = sentryEvent.Tags;

            Assert.IsNotNull(tags);
            Assert.IsNotNull(tags.SingleOrDefault(t => t.Key == "unity.gpu.supports_instancing"));
            Assert.IsNotNull(tags.SingleOrDefault(t => t.Key == "unity.device.supports_instancing"));
            Assert.IsNotNull(tags.SingleOrDefault(t => t.Key == "unity.gpu.device_type"));
            Assert.IsNotNull(tags.SingleOrDefault(t => t.Key == "unity.device.unique_identifier"));
        }

        [UnityTest]
        public IEnumerator Process_OperatingSystemProtocol_Assigned()
        {
            // setup
            var gameObject = new GameObject("ProcessorTest");
            var sentryMonoBehaviour = gameObject.AddComponent<SentryMonoBehaviour>();

            // arrange
            sentryMonoBehaviour.SentrySystemInfo = new TestSentrySystemInfo { OperatingSystem = "Windows" };

            var sentryOptions = new SentryOptions {SendDefaultPii = true};
            var application = new TestApplication();
            SentryMonoBehaviour SentryMonoBehaviourGenerator() => sentryMonoBehaviour;
            var unityEventProcessor = new UnityEventProcessor(sentryOptions, application, SentryMonoBehaviourGenerator);
            var sentryEvent = new SentryEvent();

            // act
            // SentryInitialization always called
            yield return sentryMonoBehaviour.CollectData();
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.AreEqual(sentryMonoBehaviour.SentrySystemInfo.OperatingSystem, sentryEvent.Contexts.OperatingSystem.Name);

            // TearDown
            Object.Destroy(gameObject);
        }
    }

    internal sealed class TestSentrySystemInfo : ISentrySystemInfo
    {
        public int? MainThreadId { get; }
        public string? OperatingSystem { get; set; }
        public int? ProcessorCount { get; }
        public bool? SupportsVibration { get; }
        public string? DeviceType { get; }
        public string? CpuDescription { get; }
        public string? DeviceName { get; }
        public string? DeviceUniqueIdentifier { get; }
        public string? DeviceModel { get; }
        public int? SystemMemorySize { get; }
        public int? GraphicsDeviceId { get; }
        public string? GraphicsDeviceName { get; }
        public string? GraphicsDeviceVendorId { get; }
        public string? GraphicsDeviceVendor { get; }
        public int? GraphicsMemorySize { get; }
        public bool? GraphicsMultiThreaded { get; }
        public string? NpotSupport { get; }
        public string? GraphicsDeviceVersion { get; }
        public string? GraphicsDeviceType { get; }
        public int? MaxTextureSize { get; }
        public bool? SupportsDrawCallInstancing { get; }
        public bool? SupportsRayTracing { get; }
        public bool? SupportsComputeShaders { get; }
        public bool? SupportsGeometryShaders { get; }
        public int? GraphicsShaderLevel { get; }
    }
}
