using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;

namespace Sentry.Unity.Editor.Tests
{
    public class DebugSymbolUploadTests
    {
        private class Fixture
        {
            public string FakeProjectPath { get; set; }
            public string UnityProjectPath { get; set; }
            public string GradleProjectPath { get; set; }
            public string SentryCliPath { get; set; }

            public Fixture()
            {
                FakeProjectPath = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

                UnityProjectPath = Path.Combine(FakeProjectPath, "UnityProject");
                GradleProjectPath = Path.Combine(FakeProjectPath, "GradleProject");
                SentryCliPath = Path.Combine(FakeProjectPath, "fake-sentry-cli");
            }
        }

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            SetupFakeProject(_fixture.FakeProjectPath);
        }

        private Fixture _fixture = new();

        [TearDown]
        public void TearDown() => Directory.Delete(Path.GetFullPath(_fixture.FakeProjectPath), true);

        [Test]
        public void GetSymbolsPath_IsExportingFalse_ReturnsUnityDefaultPath()
        {
            var expectedSymbolsPath = Path.Combine(_fixture.UnityProjectPath, "Temp", "StagingArea", "symbols");

            var actualSymbolsPath = DebugSymbolUpload.GetSymbolsPath(_fixture.UnityProjectPath, _fixture.GradleProjectPath, false);

            Assert.AreEqual(expectedSymbolsPath, actualSymbolsPath);
        }

        [Test]
        public void GetSymbolsPath_IsExportingTrue_CopiesSymbolsAndReturnsPath()
        {
            var expectedSymbolsPath = Path.Combine(_fixture.GradleProjectPath, DebugSymbolUpload.GradleExportedSymbolsPath);

            var actualSymbolsPath = DebugSymbolUpload.GetSymbolsPath(_fixture.UnityProjectPath, _fixture.GradleProjectPath, true);

            Assert.AreEqual(expectedSymbolsPath, actualSymbolsPath);
            Assert.IsTrue(Directory.Exists(expectedSymbolsPath));

            var files = Directory.GetFiles(expectedSymbolsPath, "*.so", SearchOption.AllDirectories).ToList();
            Assert.IsNotNull(files.Find(f => f.EndsWith("libil2cpp.dbg.so")));
            Assert.IsNotNull(files.Find(f => f.EndsWith("libil2cpp.sym.so")));
            Assert.IsNotNull(files.Find(f => f.EndsWith("libunity.sym.so")));
        }

        [Test]
        public void AppendUploadToGradleFile_SentryCliFileDoesNotExist_ThrowsFileNotFoundException()
        {
            var symbolsDirectoryPath = DebugSymbolUpload.GetSymbolsPath(_fixture.UnityProjectPath, _fixture.GradleProjectPath, false);

            Assert.Throws<FileNotFoundException>(() => DebugSymbolUpload.AppendUploadToGradleFile(string.Empty, _fixture.GradleProjectPath, symbolsDirectoryPath));
        }

        [Test]
        public void AppendUploadToGradleFile_BuildGradleFileDoesNotExist_ThrowsFileNotFoundException()
        {
            var symbolsDirectoryPath = DebugSymbolUpload.GetSymbolsPath(_fixture.UnityProjectPath, _fixture.GradleProjectPath, false);

            Assert.Throws<FileNotFoundException>(() => DebugSymbolUpload.AppendUploadToGradleFile(_fixture.SentryCliPath, string.Empty, symbolsDirectoryPath));
        }

        [Test]
        public void AppendUploadToGradleFile_SymbolsDirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() => DebugSymbolUpload.AppendUploadToGradleFile(_fixture.SentryCliPath, _fixture.GradleProjectPath, String.Empty));
        }

        [Test]
        public void AppendUploadToGradleFile_AllRequirementsMet_AppendsSentryCliToFile()
        {
            var symbolsDirectoryPath = DebugSymbolUpload.GetSymbolsPath(_fixture.UnityProjectPath, _fixture.GradleProjectPath, false);
            var expectedContent = $@"
gradle.taskGraph.whenReady {{
    gradle.taskGraph.allTasks[-1].doLast {{
        println 'Uploading symbols to Sentry'
        exec {{
            environment ""SENTRY_PROPERTIES"", ""./sentry.properties""
            executable ""{_fixture.SentryCliPath}""
            args = [""upload-dif"", ""./unityLibrary/src/main/jniLibs/""]
        }}
        exec {{
            environment ""SENTRY_PROPERTIES"", ""./sentry.properties""
            executable ""{_fixture.SentryCliPath}""
            args = [""upload-dif"", ""{symbolsDirectoryPath}""]
        }}
    }}
}}";

            DebugSymbolUpload.AppendUploadToGradleFile(_fixture.SentryCliPath, _fixture.GradleProjectPath, symbolsDirectoryPath);
            var actualContent = File.ReadAllText(Path.Combine(_fixture.GradleProjectPath, "build.gradle"));

            Assert.AreEqual(expectedContent, actualContent);
        }

        public static void SetupFakeProject(string fakeProjectPath)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectTemplatePath = Path.Combine(assemblyPath, "TestFiles", "SymbolsUploadProject");

            foreach (string dirPath in Directory.GetDirectories(projectTemplatePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(projectTemplatePath, fakeProjectPath));
            }

            foreach (string newPath in Directory.GetFiles(projectTemplatePath, "*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(projectTemplatePath, fakeProjectPath), true);
            }
        }
    }
}
