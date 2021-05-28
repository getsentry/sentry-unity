using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests
{
    public class UnityEventExceptionProcessorTests
    {
        [Test]
        public void Process_IL2CPPStackTraceFilenameWithZeroes_ShouldReturnEmptyString()
        {
            // arrange
            var unityEventProcessor = new UnityEventExceptionProcessor();
            var ill2CppUnityLogException = new UnityLogException(
                "one: two",
                "BugFarm.ThrowNull () (at <00000000000000000000000000000000>:0)");
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(ill2CppUnityLogException, sentryEvent);

            // assert
            Assert.NotNull(sentryEvent.SentryExceptions);

            var sentryException = sentryEvent.SentryExceptions!.First();
            Assert.NotNull(sentryException.Stacktrace);
            Assert.Greater(sentryException.Stacktrace!.Frames.Count, 0);

            var sentryExceptionFirstFrame = sentryException.Stacktrace!.Frames[0];
            Assert.AreEqual(string.Empty, sentryExceptionFirstFrame.FileName);
            Assert.AreEqual(string.Empty, sentryExceptionFirstFrame.AbsolutePath);
        }

        [Test]
        public void Process_SdkInfo_Correct()
        {
            // arrange
            var testApplication = new TestApplication();
            var unityEventProcessor = new UnityEventProcessor(testApplication);
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
            var unityEventProcessor = new UnityEventProcessor(testApplication);
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.AreEqual(sentryEvent.Contexts.Device.Simulator, isSimulator);
        }

        private static readonly object[] EditorSimulatorValues =
        {
            new object[] { true, true },
            new object[] { false, null! },
        };

        [Test]
        public void Process_ServerName_IsNull()
        {
            // arrange
            var unityEventProcessor = new UnityEventProcessor();
            var sentryEvent = new SentryEvent();

            // act
            unityEventProcessor.Process(sentryEvent);

            // assert
            Assert.IsNull(sentryEvent.ServerName);
        }
    }
}
