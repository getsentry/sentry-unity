using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

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
        public void CopyFrameworkToXcodeProject_CopyFramework_DirectoryExists()
        {
            var asset = new BuildAsset(_testFrameworkPath, Path.Combine(_xcodeProjectPath, "SomeDirectory"), new TestLogger());
            asset.CopyToTarget();
            Assert.IsTrue(Directory.Exists(asset.targetPath));
        }

        [Test]
        public void CopyFramework_FailedToCopy_ThrowsDirectoryNotFoundException()
        {
            var asset = new BuildAsset("non-existent-path.framework",
                    Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.framework"), new TestLogger());
            Assert.Throws<IOException>(() => asset.CopyToTarget());
        }


        [Test]
        public void CopyFile_CopyFile_FileExists()
        {
            var asset = new BuildAsset(_testFilePath, Path.Combine(_xcodeProjectPath, "SomeDirectory", "Test.m"), new TestLogger());
            asset.CopyToTarget();
            Assert.IsTrue(File.Exists(asset.targetPath));
        }
    }
}
