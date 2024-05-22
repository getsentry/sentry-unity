using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Editor.Tests.Android
{
    public class DebugSymbolUploadTests
    {
        public class Fixture
        {
            internal UnityTestLogger UnityTestLogger { get; set; }
            public string FakeProjectPath { get; set; }
            public string UnityProjectPath { get; set; }
            public string GradleProjectPath { get; set; }
            public string SentryCliPath { get; set; }

            public bool IsExporting { get; set; }
            public bool IsMinifyEnabled { get; set; }
            public TestApplication Application { get; set; }

            public Fixture()
            {
                UnityTestLogger = new();

                FakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                UnityProjectPath = Path.Combine(FakeProjectPath, "UnityProject");
                GradleProjectPath = Path.Combine(FakeProjectPath, "GradleProject");
                SentryCliPath = Path.Combine(FakeProjectPath, "fake-sentry-cli");

                Application = new TestApplication(unityVersion: "2019.4");
            }

            internal DebugSymbolUpload GetSut() => new(
                new UnityLogger(new SentryOptions(), UnityTestLogger),
                new SentryCliOptions() { UploadSources = true },
                UnityProjectPath,
                GradleProjectPath,
                IsExporting,
                IsMinifyEnabled,
                Application
            );
        }

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            SetupFakeProject(_fixture.FakeProjectPath);
        }

        private Fixture _fixture = null!; // created through SetUp

        [TearDown]
        public void TearDown() => Directory.Delete(Path.GetFullPath(_fixture.FakeProjectPath), true);

        [Test]
        [TestCase("2019.4", false)]
        [TestCase("2020.3", false)]
        [TestCase("2021.1", false)]
        [TestCase("2021.2", true)]
        [TestCase("2022.1", true)]
        public void IsNewBuildingBackend(string unityVersion, bool expectedIsNewBuildingBackend)
        {
            _fixture.Application = new TestApplication(unityVersion: unityVersion);

            var actualIsNewBuildingBackend = DebugSymbolUpload.IsNewBuildingBackend(_fixture.Application);

            Assert.AreEqual(expectedIsNewBuildingBackend, actualIsNewBuildingBackend);
        }

        [Test]
        [TestCase("2019.4", DebugSymbolUpload.RelativeBuildOutputPathOld, DebugSymbolUpload.RelativeGradlePathOld)]
        [TestCase("2021.2", DebugSymbolUpload.RelativeBuildOutputPathNew, DebugSymbolUpload.RelativeAndroidPathNew)]
        public void GetSymbolUploadPaths_IsExportingFalse_ReturnsCorrectPathForVersion(string unityVersion,
            string relativeBuildPath, string gradlePath)
        {
            _fixture.IsExporting = false;
            var sut = _fixture.GetSut();

            var actualSymbolsPaths = sut.GetSymbolUploadPaths();

            Assert.NotNull(actualSymbolsPaths.Any(path => path.Contains(relativeBuildPath)));
            Assert.NotNull(actualSymbolsPaths.Any(path => path.Contains(gradlePath)));
        }

        [Test]
        public void GetSymbolUploadPaths_IsExportingTrue_ReturnsPathToExportedProject()
        {
            _fixture.IsExporting = true;
            var sut = _fixture.GetSut();

            var actualSymbolPaths = sut.GetSymbolUploadPaths();

            Assert.AreEqual(2, actualSymbolPaths.Count);
        }

        [Test]
        public void AppendUploadToGradleFile_SentryCliFileDoesNotExist_ThrowsFileNotFoundException()
        {
            var invalidSentryCliPath = Path.GetRandomFileName();
            var sut = _fixture.GetSut();

            var ex = Assert.Throws<FileNotFoundException>(() => sut.AppendUploadToGradleFile(invalidSentryCliPath));
            Assert.AreEqual(invalidSentryCliPath, ex.FileName);
        }

        [Test]
        public void AppendUploadToGradleFile_BuildGradleFileDoesNotExist_ThrowsFileNotFoundException()
        {
            _fixture.GradleProjectPath = Path.GetRandomFileName();
            var sut = _fixture.GetSut();

            var ex = Assert.Throws<FileNotFoundException>(() => sut.AppendUploadToGradleFile(_fixture.SentryCliPath));
            Assert.AreEqual(GetGradleFilePath(), ex.FileName);
        }

        [Test]
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        public void AppendUploadToGradleFile_AllRequirementsMet_AppendsUploadTask(bool isExporting, bool addMapping)
        {
            _fixture.IsExporting = isExporting;
            _fixture.IsMinifyEnabled = addMapping;
            var sut = _fixture.GetSut();

            sut.AppendUploadToGradleFile(_fixture.SentryCliPath);
            var actualFileContent = File.ReadAllText(GetGradleFilePath());

            const string mappingTaskBuild = @"
        println 'Uploading mapping file to Sentry.'
        def mappingLogFile = new FileOutputStream\('[^\s]+\/UnityProject\/Logs\/sentry-mapping-upload\.log'\)
        exec {
            environment 'SENTRY_PROPERTIES', file\(""\${rootDir}\/sentry\.properties""\)\.absolutePath
            executable '[^\]]+[^.]\/fake-sentry-cli'
            args = \['upload-proguard', [^\]]+\]
            standardOutput mappingLogFile
            errorOutput mappingLogFile
        }";

            const string mappingTaskExport = @"
        println 'Uploading mapping file to Sentry.'
        exec {
            environment 'SENTRY_PROPERTIES', file\(""\${rootDir}\/sentry\.properties""\)\.absolutePath
            executable file\(""\${rootDir}\/fake-sentry-cli""\)\.absolutePath
            args = \['upload-proguard', (.*?)\]
        }";

            var mappingTask = string.Empty;
            if (addMapping)
                mappingTask = isExporting ? mappingTaskExport : mappingTaskBuild;

            const string taskFormatExport = @"\/\/ Autogenerated Sentry symbol upload task \[start\]
\/\/ Credentials and project settings information are stored in the sentry\.properties file
afterEvaluate {
task sentryUploadSymbols {
    doLast {
        println 'Uploading symbols to Sentry\.'
        exec {
            environment 'SENTRY_PROPERTIES', file\(""\${rootDir}\/sentry\.properties""\)\.absolutePath
            executable file\(""\${rootDir}\/fake-sentry-cli""\)\.absolutePath
            args = \['debug-files', 'upload', '--il2cpp-mapping', '--include-sources', project\.rootDir\]
        }{0}
    }
}

tasks\.assembleDebug\.finalizedBy sentryUploadSymbols
tasks\.assembleRelease\.finalizedBy sentryUploadSymbols
}
\/\/ Autogenerated Sentry symbol upload task \[end\]";

            const string taskFormatBuild = @"\/\/ Autogenerated Sentry symbol upload task \[start\]
\/\/ Credentials and project settings information are stored in the sentry\.properties file
afterEvaluate {
task sentryUploadSymbols {
    doLast {
        println 'Uploading symbols to Sentry\. You can find the full log in \.\/Logs\/sentry-symbols-upload\.log \(the file content may not be strictly sequential because it\\'s a merge of two streams\)\.'
        def sentryLogFile = new FileOutputStream\('[^\s]+\/UnityProject\/Logs\/sentry-symbols-upload\.log'\)
        exec {
            environment 'SENTRY_PROPERTIES', file\(""\${rootDir}\/sentry\.properties""\)\.absolutePath
            executable '[^\]]+[^.]\/fake-sentry-cli'
            args = \['debug-files', 'upload', '--il2cpp-mapping', '--include-sources', [^\]]+\]
            standardOutput sentryLogFile
            errorOutput sentryLogFile
        }{0}
    }
}

tasks\.assembleDebug\.finalizedBy sentryUploadSymbols
tasks\.assembleRelease\.finalizedBy sentryUploadSymbols
}
\/\/ Autogenerated Sentry symbol upload task \[end\]";

            var format = isExporting ? taskFormatExport : taskFormatBuild;
            var taskString = format.Replace("{0}", mappingTask);
            StringAssert.IsMatch(taskString.Replace("\r", ""), actualFileContent.Replace("\r", ""));
        }

        [Test]
        public void RemoveUploadTaskFromGradleFile_GradleFileDoesNotExist_ThrowsFileNotFoundException()
        {
            _fixture.GradleProjectPath = Path.GetRandomFileName();
            var sut = _fixture.GetSut();

            var ex = Assert.Throws<FileNotFoundException>(() => sut.RemoveUploadFromGradleFile());
            Assert.AreEqual(GetGradleFilePath(), ex.FileName);
        }

        [Test]
        public void RemoveUploadTaskFromGradleFile_UploadHasNotBeenAdded_LogsAndReturns()
        {
            var sut = _fixture.GetSut();

            sut.RemoveUploadFromGradleFile();

            _fixture.UnityTestLogger.AssertLogContains(SentryLevel.Debug, "No previous upload task found.");
        }

        [Test]
        public void RemoveUploadTaskFromGradleFile_UploadHasBeenAdded_RemovesUploadTask()
        {
            var sut = _fixture.GetSut();
            sut.AppendUploadToGradleFile(_fixture.SentryCliPath);

            // Sanity check
            var actualFileContent = File.ReadAllText(GetGradleFilePath());
            StringAssert.Contains("sentry.properties", actualFileContent);

            sut.RemoveUploadFromGradleFile();

            actualFileContent = File.ReadAllText(GetGradleFilePath());
            StringAssert.DoesNotContain("sentry.properties", actualFileContent);
        }

        [Test]
        [TestCase("2019.4")]
        [TestCase("2020.3")]
        [TestCase("2021.1")]
        [TestCase("2021.2")]
        [TestCase("2022.1")]
        public void TryCopySymbolsToGradleProject_CopiesFilesFromBuildOutputToSymbolsDirectory(string unityVersion)
        {
            _fixture.Application = new TestApplication(unityVersion: unityVersion);
            _fixture.IsExporting = true;
            var expectedSymbolsPath = Path.Combine(_fixture.GradleProjectPath, "symbols");
            var sut = _fixture.GetSut();

            sut.TryCopySymbolsToGradleProject(_fixture.Application);

            var files = Directory.GetFiles(expectedSymbolsPath, "*.so", SearchOption.AllDirectories).ToList();
            Assert.IsNotNull(files.Find(f => f.EndsWith("libil2cpp.dbg.so")));
            Assert.IsNotNull(files.Find(f => f.EndsWith("libil2cpp.sym.so")));
            Assert.IsNotNull(files.Find(f => f.EndsWith("libunity.sym.so")));
        }

        public static void SetupFakeProject(string fakeProjectPath)
        {
            Directory.CreateDirectory(fakeProjectPath);

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectTemplatePath = Path.Combine(assemblyPath, "TestFiles", "SymbolsUploadProject");

            foreach (var dirPath in Directory.GetDirectories(projectTemplatePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(projectTemplatePath, fakeProjectPath));
            }

            foreach (var newPath in Directory.GetFiles(projectTemplatePath, "*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(projectTemplatePath, fakeProjectPath), true);
            }
        }

        private string GetGradleFilePath() => Path.Combine(_fixture.GradleProjectPath, "launcher/build.gradle");
    }
}
