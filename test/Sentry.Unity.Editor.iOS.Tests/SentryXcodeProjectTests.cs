using System.IO;
using System.Reflection;
using NUnit.Framework;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class SentryXcodeProjectTests
    {
        private class NativeMainTest : INativeMain
        {
            public void AddSentry(string pathToMain, IDiagnosticLogger? logger) { }
        }

        private class NativeOptionsTest : INativeOptions
        {
            public void CreateFile(string path, SentryOptions options) { }
        }

        private class Fixture
        {
            public string ProjectRoot { get; set; } =
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestFiles", "2019_4");
            public SentryUnityOptions Options { get; set; } = new();
            public INativeMain NativeMain { get; set; } = new NativeMainTest();
            public INativeOptions NativeOptions { get; set; } = new NativeOptionsTest();

            public SentryXcodeProject GetSut() => new(ProjectRoot, NativeMain, NativeOptions);
        }

        private Fixture _fixture = new();

        [SetUp]
        public void SetUp() => _fixture = new Fixture();

        [TearDown]
        public void DestroyFrameworkDirectories()
        {
            var frameworkPath = Path.Combine(_fixture.ProjectRoot, "Frameworks");
            if (Directory.Exists(frameworkPath))
            {
                Directory.Delete(frameworkPath, true);
            }
        }

        [Test]
        public void ReadFromProjectFile_ProjectExists_ReadsProject()
        {
            var xcodeProject = _fixture.GetSut();

            xcodeProject.ReadFromProjectFile();

            Assert.IsNotEmpty(xcodeProject.ProjectToString());
        }

        [Test]
        public void ReadFromProjectFile_ProjectDoesNotExist_ThrowsFileNotFoundException()
        {
            _fixture.ProjectRoot = "Path/That/Does/Not/Exist";
            var xcodeProject = _fixture.GetSut();

            Assert.Throws<FileNotFoundException>(() => xcodeProject.ReadFromProjectFile());
        }

        [Test]
        public void SetRelativeFrameworkPath_PathExists_ReturnsFrameworkPath()
        {
            CreateFrameworkDirectories();

            var xcodeProject = _fixture.GetSut();
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity", "Plugins", "iOS");

            xcodeProject.SetRelativeFrameworkPath();

            Assert.AreEqual(expectedFrameworkPath, xcodeProject.RelativeFrameworkPath);
        }

        [Test]
        public void SetRelativeFrameworkPath_DevPathExists_ReturnsDevFrameworkPath()
        {
            CreateDevFrameworkDirectories();

            var xcodeProject = _fixture.GetSut();
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity.dev", "Plugins", "iOS");

            xcodeProject.SetRelativeFrameworkPath();

            Assert.AreEqual(expectedFrameworkPath, xcodeProject.RelativeFrameworkPath);
        }

        [Test]
        public void SetRelativeFrameworkPath_PathDoesNotExist_ThrowsFileNotFoundException()
        {
            _fixture.ProjectRoot = "Path/That/Does/Not/Exist";
            var xcodeProject = _fixture.GetSut();

            Assert.Throws<FileNotFoundException>(() => xcodeProject.SetRelativeFrameworkPath());
        }

        [Test]
        public void AddSentryFramework_CleanXcodeProject_SentryWasAdded()
        {
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();
            xcodeProject.RelativeFrameworkPath = "Frameworks/io.sentry.unity/Plugins/iOS/";

            xcodeProject.AddSentryFramework();

            StringAssert.Contains(@"OTHER_LDFLAGS = ""-ObjC"";", xcodeProject.ProjectToString());
        }

        [Test]
        public void CreateNativeOptions_CleanXcodeProject_NativeOptionsAdded()
        {
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();
            xcodeProject.RelativeFrameworkPath = "Frameworks/io.sentry.unity/Plugins/iOS/";

            xcodeProject.AddNativeOptions(_fixture.Options);

            StringAssert.Contains("SentryOptions.m", xcodeProject.ProjectToString());
        }

        private void CreateFrameworkDirectories()
        {
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(_fixture.ProjectRoot, expectedFrameworkPath));
        }

        private void CreateDevFrameworkDirectories()
        {
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity.dev", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(_fixture.ProjectRoot, expectedFrameworkPath));
        }
    }
}
