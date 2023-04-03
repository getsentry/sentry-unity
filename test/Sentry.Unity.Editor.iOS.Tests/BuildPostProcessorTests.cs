using System;
using System.IO;
using System.Linq;
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
        private string _xcodeProjectPath = null!;

        [SetUp]
        public void Setup()
        {
            _testDirectoryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Substring(0, 4));
            _testFrameworkPath = Path.Combine(_testDirectoryRoot, "Test.framework");
            _testFilePath = Path.Combine(_testDirectoryRoot, "Test.m");
            _xcodeProjectPath = Path.Combine(_testDirectoryRoot, "XcodeProject");

            Directory.CreateDirectory(_testDirectoryRoot);
            Directory.CreateDirectory(_testFrameworkPath);
            File.Create(_testFilePath).Close();
            Directory.CreateDirectory(_xcodeProjectPath);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_testDirectoryRoot, true);

        [Test]
        public void AddSentryToXcodeProject_OptionsNull_LogsAndReturn()
        {
            var options = new SentryUnityOptions { Enabled = false };
            var testLogger = new TestLogger();

            BuildPostProcess.AddSentryToXcodeProject(null, null, testLogger, string.Empty);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Warning &&
                log.message.Contains("iOS native support disabled because Sentry has not been configured.")));
            Assert.IsFalse(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Info &&
                log.message.Contains("Attempting to add Sentry to the Xcode project.")));
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
            var file = new FileInfo(Path.Combine(_xcodeProjectPath, SentryXcodeProject.MainPath));
            file.Directory?.Create();
            File.WriteAllText(file.FullName, NativeMain.Include);
            var options = new SentryUnityOptions
            {
                Dsn = "test_dsn",
                IosNativeSupportEnabled = false
            };
            var testLogger = new TestLogger();

            Assert.Throws<BuildFailedException>(() =>
                BuildPostProcess.AddSentryToXcodeProject(options, null, testLogger, _xcodeProjectPath));

            Directory.Delete(_xcodeProjectPath, true);
        }

        [Test]
        public void CopyFrameworkToXcodeProject_CopyFramework_DirectoryExists()
        {
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework");

            BuildPostProcess.CopyFramework(_testFrameworkPath, targetPath, new TestLogger());

            Assert.IsTrue(Directory.Exists(targetPath));
        }

        [Test]
        public void CopyFramework_FrameworkAlreadyExists_LogsSkipMessage()
        {
            var testLogger = new TestLogger();
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework");

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
                    Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework"), new TestLogger()));

        [Test]
        public void CopyFramework_FailedToCopy_ThrowsDirectoryNotFoundException() =>
            Assert.Throws<DirectoryNotFoundException>(() =>
                BuildPostProcess.CopyFramework("non-existent-path.framework",
                    Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework"), new TestLogger()));


        [Test]
        public void CopyFile_CopyFile_FileExists()
        {
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.m");

            BuildPostProcess.CopyFile(_testFilePath, targetPath, new TestLogger());

            Assert.IsTrue(File.Exists(targetPath));
        }

        [Test]
        public void CopyFile_FileAlreadyExists_LogsSkipMessage()
        {
            var testLogger = new TestLogger();
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.m");

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
                    Path.Combine(_xcodeProjectPath, "NewDirectory", "Test.m"), new TestLogger()));

        [Test]
        public void CopyFile_FailedToCopyFile_ThrowsFileNotFoundException() =>
            Assert.Throws<FileNotFoundException>(() =>
                BuildPostProcess.CopyFile("non-existent-path.m",
                    Path.Combine(_xcodeProjectPath, "NewDirectory", "Test.m"), new TestLogger()));
    }
}
