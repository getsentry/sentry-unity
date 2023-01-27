using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEditor;

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

            internal DebugSymbolUpload GetSut() => new(new UnityLogger(new SentryOptions(), UnityTestLogger),
                null, UnityProjectPath, GradleProjectPath, ScriptingImplementation.IL2CPP, IsExporting, Application);
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
            var sut = _fixture.GetSut();

            var actualSymbolsPaths = sut.GetSymbolUploadPaths(false);

            Assert.NotNull(actualSymbolsPaths.Any(path => path.Contains(relativeBuildPath)));
            Assert.NotNull(actualSymbolsPaths.Any(path => path.Contains(gradlePath)));
        }

        [Test]
        public void GetSymbolUploadPaths_IsExportingTrue_ReturnsPathToExportedProject()
        {
            var sut = _fixture.GetSut();

            var actualSymbolPaths = sut.GetSymbolUploadPaths(true);

            Assert.AreEqual(1, actualSymbolPaths.Length);
            Assert.NotNull(actualSymbolPaths.Any(path => path.Contains(_fixture.GradleProjectPath)));
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
            Assert.AreEqual(Path.Combine(_fixture.GradleProjectPath, "build.gradle"), ex.FileName);
        }

        [Test]
        public void AppendUploadToGradleFile_SymbolsDirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
        {
            var sut = _fixture.GetSut();
            sut._symbolUploadPaths = new[] { string.Empty };

            Assert.Throws<DirectoryNotFoundException>(() => sut.AppendUploadToGradleFile(_fixture.SentryCliPath));
        }

        [Test]
        public void AppendUploadToGradleFile_AllRequirementsMet_AppendsUploadTask()
        {
            var sut = _fixture.GetSut();

            sut.AppendUploadToGradleFile(_fixture.SentryCliPath);
            var actualFileContent = File.ReadAllText(Path.Combine(_fixture.GradleProjectPath, "build.gradle"));

            StringAssert.Contains("sentry.properties", actualFileContent);
        }

        [Test]
        public void RemoveUploadTaskFromGradleFile_GradleFileDoesNotExist_ThrowsFileNotFoundException()
        {
            _fixture.GradleProjectPath = Path.GetRandomFileName();
            var sut = _fixture.GetSut();

            var ex = Assert.Throws<FileNotFoundException>(() => sut.RemoveUploadFromGradleFile());
            Assert.AreEqual(Path.Combine(_fixture.GradleProjectPath, "build.gradle"), ex.FileName);
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
            var actualFileContent = File.ReadAllText(Path.Combine(_fixture.GradleProjectPath, "build.gradle"));
            StringAssert.Contains("sentry.properties", actualFileContent);

            sut.RemoveUploadFromGradleFile();

            actualFileContent = File.ReadAllText(Path.Combine(_fixture.GradleProjectPath, "build.gradle"));
            StringAssert.DoesNotContain("sentry.properties", actualFileContent);
        }

        [Test]
        public void TryCopySymbolsToGradleProject_IsNewBuildingBackend_LogsAndReturns()
        {
            _fixture.Application = new TestApplication(unityVersion: "2021.2");

            var sut = _fixture.GetSut();

            sut.TryCopySymbolsToGradleProject(_fixture.Application);

            _fixture.UnityTestLogger.AssertLogContains(SentryLevel.Debug,
                "New building backend. Skipping copying of debug symbols.");
        }

        [Test]
        public void TryCopySymbolsToGradleProject_IsOldBuildingBackend_CopiesFilesFromBuildOutputToSymbolsDirectory()
        {
            _fixture.Application = new TestApplication(unityVersion: "2019.4");
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
    }
}
