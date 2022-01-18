using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class BuildPostProcessorTests
    {
        private string _testDirectoryPath = null!;
        private string _sentryFrameworkPath = null!;
        private string _xcodeProjectPath = null!;

        [SetUp]
        public void Setup()
        {
            _testDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Substring(0, 4));
            _sentryFrameworkPath = Path.Combine(_testDirectoryPath, "Sentry.framework");
            _xcodeProjectPath = Path.Combine(_testDirectoryPath, "XcodeProject");

            Directory.CreateDirectory(_testDirectoryPath);
            Directory.CreateDirectory(_sentryFrameworkPath);
            Directory.CreateDirectory(_xcodeProjectPath);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_testDirectoryPath, true);

        [Test]
        public void CopyFrameworkToBuildDirectory_CopiesFramework()
        {
            BuildPostProcess.CopyFrameworkToBuildDirectory(_xcodeProjectPath, _sentryFrameworkPath, new TestLogger());

            Assert.IsTrue(Directory.Exists(Path.Combine(_xcodeProjectPath, "Frameworks", "Sentry.framework")));
        }

        [Test]
        public void CopyFrameworkToBuildDirectory_FrameworkAlreadyCopied_LogsSkipMessage()
        {
            var testLogger = new TestLogger();

            BuildPostProcess.CopyFrameworkToBuildDirectory(_xcodeProjectPath, _sentryFrameworkPath, testLogger);
            BuildPostProcess.CopyFrameworkToBuildDirectory(_xcodeProjectPath, _sentryFrameworkPath, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("'Sentry.framework' has already copied")));
        }

        [Test]
        public void CopyFrameworkToBuildDirectory_FailedToCopyFramework_ThrowsFileNotFoundException() =>
            Assert.Throws<FileNotFoundException>(() =>
                BuildPostProcess.CopyFrameworkToBuildDirectory(_xcodeProjectPath, "non-existent-path", new TestLogger()));
    }
}
