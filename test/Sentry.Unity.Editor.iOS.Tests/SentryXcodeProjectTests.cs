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
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestFiles");
            public SentryUnityOptions Options { get; set; } = new();
            public INativeMain NativeMain { get; set; } = new NativeMainTest();
            public INativeOptions NativeOptions { get; set; } = new NativeOptionsTest();

            public SentryXcodeProject GetSut() => new(ProjectRoot, Options, NativeMain, NativeOptions);
        }

        private readonly Fixture _fixture = new();

        [Test]
        public void SetRelativeFrameworkPath_PathExists_ReturnsFrameworkPath()
        {
            CreateFrameworkFolder();

            var xcodeProject = _fixture.GetSut();
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity", "Plugins", "iOS");

            xcodeProject.SetRelativeFrameworkPath();

            Assert.AreEqual(expectedFrameworkPath, xcodeProject.RelativeFrameworkPath);
        }

        [Test]
        public void SetRelativeFrameworkPath_DevPathExists_ReturnsDevFrameworkPath()
        {
            CreateDevFrameworkFolder();

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

            xcodeProject.AddNativeOptions();

            StringAssert.Contains("SentryOptions.m", xcodeProject.ProjectToString());
        }


        private void CreateFrameworkFolder()
        {
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(_fixture.ProjectRoot, expectedFrameworkPath));
        }

        private void CreateDevFrameworkFolder()
        {
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity.dev", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(_fixture.ProjectRoot, expectedFrameworkPath));
        }

        [TearDown]
        public void DestroyFolder()
        {
            var frameworkPath = Path.Combine(_fixture.ProjectRoot, "Frameworks");
            if (Directory.Exists(frameworkPath))
            {
                Directory.Delete(frameworkPath, true);
            }
        }
    }
}
