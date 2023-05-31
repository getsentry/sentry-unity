using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using UnityEditor.Build;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class BuildPostProcessorTests
    {
        private string _testDirectoryRoot = null!;
        private string _testFrameworkPath = null!;
        private string _testFilePath = null!;
        private string _outputProjectPath = null!;

        [SetUp]
        public void Setup()
        {
            _testDirectoryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Substring(0, 4));
            _testFrameworkPath = Path.Combine(_testDirectoryRoot, "Test.framework");
            _testFilePath = Path.Combine(_testDirectoryRoot, "Test.m");
            _outputProjectPath = Path.Combine(_testDirectoryRoot, "XcodeProject");

            Directory.CreateDirectory(_testDirectoryRoot);
            Directory.CreateDirectory(_testFrameworkPath);
            File.Create(_testFilePath).Close();

            // Test setup for output Xcode project
            var testXcodeProjectPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestFiles", "2019_4");
            SentryFileUtil.CopyDirectory(testXcodeProjectPath, _outputProjectPath);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_testDirectoryRoot, true);

        [Test]
        public void AddSentryToXcodeProject_OptionsNull_LogsAndCopiesNoOpBridge()
        {
            var testLogger = new TestLogger();

            BuildPostProcess.AddSentryToXcodeProject(null, null, testLogger, _outputProjectPath);

            Assert.IsFalse(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Info &&
                log.message.Contains("Attempting to add Sentry to the Xcode project."))); // Sanity check
            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Warning &&
                log.message.Contains("iOS native support disabled because Sentry has not been configured.")));

            var noOpBridgePath = Path.Combine(_outputProjectPath, "Libraries", SentryPackageInfo.GetName(),
                SentryXcodeProject.BridgeName);

            Assert.IsTrue(File.Exists(noOpBridgePath));
            StringAssert.DoesNotContain("[SentrySDK", File.ReadAllText(noOpBridgePath)); // The NoOp bridge does not call into the Cocoa SDK
        }

        [Test]
        public void AddSentryToXcodeProject_SdkDisabled_LogsAndCopiesNoOpBridge()
        {
            var options = new SentryUnityOptions { Enabled = false };
            var testLogger = new TestLogger();

            BuildPostProcess.AddSentryToXcodeProject(options, null, testLogger, _outputProjectPath);

            Assert.IsFalse(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Info &&
                log.message.Contains("Attempting to add Sentry to the Xcode project."))); // Sanity check
            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Warning &&
                log.message.Contains("Sentry SDK has been disabled. There will be no iOS native support.")));

            var noOpBridgePath = Path.Combine(_outputProjectPath, "Libraries", SentryPackageInfo.GetName(),
                SentryXcodeProject.BridgeName);

            Assert.IsTrue(File.Exists(noOpBridgePath));
            StringAssert.DoesNotContain("[SentrySDK", File.ReadAllText(noOpBridgePath)); // The NoOp bridge does not call into the Cocoa SDK
        }

        [Test]
        public void SetupNoOpBridge_CopiesNoOpBridgeToOutput()
        {
            BuildPostProcess.SetupNoOpBridge(new TestLogger(), _outputProjectPath);

            var noOpBridgePath = Path.Combine(_outputProjectPath, "Libraries", SentryPackageInfo.GetName(),
                SentryXcodeProject.BridgeName);

            Assert.IsTrue(File.Exists(noOpBridgePath));
            StringAssert.DoesNotContain("[SentrySDK", File.ReadAllText(noOpBridgePath)); // The NoOp bridge does not call into the Cocoa SDK
        }

        [Test]
        public void SetupSentry_CopiesFrameworkAndBridge()
        {
            var options = new SentryUnityOptions();
            var testLogger = new TestLogger();

            BuildPostProcess.SetupSentry(options, null, testLogger, _outputProjectPath);

            var bridgePath = Path.Combine(_outputProjectPath, "Libraries", SentryPackageInfo.GetName(),
                SentryXcodeProject.BridgeName);
            var frameworkPath = Path.Combine(_outputProjectPath, "Frameworks", SentryXcodeProject.FrameworkName);

            Assert.IsTrue(File.Exists(bridgePath));
            Assert.IsTrue(Directory.Exists(frameworkPath));

            StringAssert.Contains("[SentrySDK", File.ReadAllText(bridgePath)); // Sanity check
        }

        [Test]
        public void IsNativeSupportEnabled_OptionsDisabled_LogsAndReturnsFalse()
        {
            var options = new SentryUnityOptions { Enabled = false };
            var testLogger = new TestLogger();

            var enabled = BuildPostProcess.IsNativeSupportEnabled(options, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Warning &&
                log.message.Contains("Sentry SDK has been disabled. There will be no iOS native support.")));
            Assert.IsFalse(enabled);
        }

        [Test]
        public void IsNativeSupportEnabled_IosNativeSupportDisabled_LogsAndReturnsFalse()
        {
            var options = new SentryUnityOptions
            {
                Dsn = "test_dsn",
                IosNativeSupportEnabled = false
            };
            var testLogger = new TestLogger();

            var enabled = BuildPostProcess.IsNativeSupportEnabled(options, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Info &&
                log.message.Contains("The iOS native support has been disabled through the options.")));
            Assert.IsFalse(enabled);
        }

        [Test]
        public void AddSentryToXcodeProject_NativeSupportDisabledButMainAlreadyModified_ThrowsBuildFailedException()
        {
            var file = new FileInfo(Path.Combine(_outputProjectPath, SentryXcodeProject.MainPath));
            file.Directory?.Create();
            File.WriteAllText(file.FullName, NativeMain.Include);
            var options = new SentryUnityOptions
            {
                Dsn = "test_dsn",
                IosNativeSupportEnabled = false
            };
            var testLogger = new TestLogger();

            Assert.Throws<BuildFailedException>(() =>
                BuildPostProcess.AddSentryToXcodeProject(options, null, testLogger, _outputProjectPath));

            Directory.Delete(_outputProjectPath, true);
        }

        [Test]
        public void CopyFrameworkToXcodeProject_CopyFramework_DirectoryExists()
        {
            var targetPath = Path.Combine(_outputProjectPath, "SomeDirectory", "Test.framework");

            BuildPostProcess.CopyFramework(_testFrameworkPath, targetPath, new TestLogger());

            Assert.IsTrue(Directory.Exists(targetPath));
        }

        [Test]
        public void CopyFramework_FrameworkAlreadyExists_LogsSkipMessage()
        {
            var testLogger = new TestLogger();
            var targetPath = Path.Combine(_outputProjectPath, "SomeDirectory", "Test.framework");

            BuildPostProcess.CopyFramework(_testFrameworkPath, targetPath, testLogger);
            BuildPostProcess.CopyFramework(_testFrameworkPath, targetPath, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("has already been copied to")));
        }

        [Test]
        public void CopyFramework_SourceMissing_ThrowsDirectoryNotFoundException() =>
            Assert.Throws<DirectoryNotFoundException>(() =>
                BuildPostProcess.CopyFramework("non-existent-path.framework",
                    Path.Combine(_outputProjectPath, "SomeDirectory", "Test.framework"), new TestLogger()));

        [Test]
        public void CopyFramework_FailedToCopy_ThrowsDirectoryNotFoundException() =>
            Assert.Throws<DirectoryNotFoundException>(() =>
                BuildPostProcess.CopyFramework("non-existent-path.framework",
                    Path.Combine(_outputProjectPath, "SomeDirectory", "Test.framework"), new TestLogger()));

        [Test]
        public void CopyFile_CopyFile_FileExists()
        {
            var targetPath = Path.Combine(_outputProjectPath, "SomeDirectory", "Test.m");

            BuildPostProcess.CopyFile(_testFilePath, targetPath, new TestLogger());

            Assert.IsTrue(File.Exists(targetPath));
        }

        [Test]
        public void CopyFile_FileAlreadyExists_LogsSkipMessage()
        {
            var testLogger = new TestLogger();
            var targetPath = Path.Combine(_outputProjectPath, "SomeDirectory", "Test.m");

            BuildPostProcess.CopyFile(_testFilePath, targetPath, testLogger);
            BuildPostProcess.CopyFile(_testFilePath, targetPath, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("has already been copied to")));
        }

        [Test]
        public void CopyFile_SourceMissing_ThrowsFileNotFoundException() =>
            Assert.Throws<FileNotFoundException>(() =>
                BuildPostProcess.CopyFile("non-existent-path.m",
                    Path.Combine(_outputProjectPath, "NewDirectory", "Test.m"), new TestLogger()));

        [Test]
        public void CopyFile_FailedToCopyFile_ThrowsFileNotFoundException() =>
            Assert.Throws<FileNotFoundException>(() =>
                BuildPostProcess.CopyFile("non-existent-path.m",
                    Path.Combine(_outputProjectPath, "NewDirectory", "Test.m"), new TestLogger()));
    }
}
