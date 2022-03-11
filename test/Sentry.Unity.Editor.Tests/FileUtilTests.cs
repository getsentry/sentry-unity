using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests
{
    public class EditorFileIOTests
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
        public void CopyFrameworkToXcodeProject_CopyFramework_DirectoryExists()
        {
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework");

            EditorFileIO.CopyDirectory(_testFrameworkPath, targetPath, new TestLogger());

            Assert.IsTrue(Directory.Exists(targetPath));
        }

        [Test]
        public void CopyFramework_FrameworkAlreadyExists_LogsSkipMessage()
        {
            var testLogger = new TestLogger();
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework");

            EditorFileIO.CopyDirectory(_testFrameworkPath, targetPath, testLogger);
            EditorFileIO.CopyDirectory(_testFrameworkPath, targetPath, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("has already been copied to")));
        }

        [Test]
        public void CopyFramework_FailedToCopy_ThrowsDirectoryNotFoundException() =>
            Assert.Throws<DirectoryNotFoundException>(() =>
                EditorFileIO.CopyDirectory("non-existent-path.framework",
                    Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework"), new TestLogger()));


        [Test]
        public void CopyFile_CopyFile_FileExists()
        {
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.m");

            EditorFileIO.CopyFile(_testFilePath, targetPath, new TestLogger());

            Assert.IsTrue(File.Exists(targetPath));
        }

        [Test]
        public void CopyFile_FileAlreadyExists_LogsSkipMessage()
        {
            var testLogger = new TestLogger();
            var targetPath = Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.m");

            EditorFileIO.CopyFile(_testFilePath, targetPath, testLogger);
            EditorFileIO.CopyFile(_testFilePath, targetPath, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("has already been copied to")));
        }

        [Test]
        public void CopyFile_FailedToCopyFile_ThrowsFileNotFoundException() =>
            Assert.Throws<FileNotFoundException>(() =>
                EditorFileIO.CopyFile("non-existent-path.m",
                    Path.Combine(_xcodeProjectPath, "NewDirectory", "Test.m"), new TestLogger()));
    }
}
