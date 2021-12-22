using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class BuildPostProcessorTests
    {
        public class Fixture
        {
            public string TestDirectoryPath { get; set;}
            public string SentryFrameworkPath { get; set; }
            public string XcodeProjectPath { get; set; }

            public Fixture()
            {
                TestDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Substring(0, 4));
                SentryFrameworkPath = Path.Combine(TestDirectoryPath, "Sentry.framework");
                XcodeProjectPath = Path.Combine(TestDirectoryPath, "XcodeProject");
            }
        }

        private Fixture _fixture = null!;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();

            Directory.CreateDirectory(_fixture.TestDirectoryPath);
            Directory.CreateDirectory(_fixture.SentryFrameworkPath);
            Directory.CreateDirectory(_fixture.XcodeProjectPath);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_fixture.TestDirectoryPath, true);

        [Test]
        public void CopyFrameworkToBuildDirectory_CopiesFramework()
        {
            BuildPostProcess.CopyFrameworkToBuildDirectory(_fixture.XcodeProjectPath, _fixture.SentryFrameworkPath, new TestLogger());

            Assert.IsTrue(Directory.Exists(Path.Combine(_fixture.XcodeProjectPath, "Frameworks", "Sentry.framework")));
        }

        [Test]
        public void CopyFrameworkToBuildDirectory_FrameworkAlreadyCopied_LogsSkipMessage()
        {
            var testLogger = new TestLogger();

            BuildPostProcess.CopyFrameworkToBuildDirectory(_fixture.XcodeProjectPath, _fixture.SentryFrameworkPath, testLogger);
            BuildPostProcess.CopyFrameworkToBuildDirectory(_fixture.XcodeProjectPath, _fixture.SentryFrameworkPath, testLogger);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("'Sentry.framework' has already copied")));
        }

        [Test]
        public void CopyFrameworkToBuildDirectory_FailedToCopyFramework_ThrowsFileNotFoundException()
        {
            _fixture.SentryFrameworkPath = "non-existent-path";

            Assert.Throws<IOException>(() =>
                BuildPostProcess.CopyFrameworkToBuildDirectory(_fixture.XcodeProjectPath, _fixture.SentryFrameworkPath, new TestLogger()));
        }
    }
}
