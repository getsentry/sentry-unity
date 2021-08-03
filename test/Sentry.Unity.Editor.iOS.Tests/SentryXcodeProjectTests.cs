using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class NativeMainFixture : INativeMain
    {
        public void AddSentry(string pathToMain) { }
    }

    public class NativeOptionsFixture : ISentryNativeOptions
    {
        public void CreateFile(SentryOptions options, string path) { }
    }

    public class SentryXcodeProjectTests
    {
        [Test]
        public void GetFrameworkPath_PathExists_ReturnsFrameworkPath()
        {
            string root = Path.GetTempPath() + Path.GetRandomFileName();
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(root, expectedFrameworkPath));

            var actualFrameworkPath = SentryXcodeProject.GetFrameworkPath(root);

            Assert.AreEqual(expectedFrameworkPath, actualFrameworkPath);

            Directory.Delete(root, true);
        }

        [Test]
        public void GetFrameworkPath_DevPathExists_ReturnsDevFrameworkPath()
        {
            string root = Path.GetTempPath() + Path.GetRandomFileName();
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity.dev", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(root, expectedFrameworkPath));

            var actualFrameworkPath = SentryXcodeProject.GetFrameworkPath(root);

            Assert.AreEqual(expectedFrameworkPath, actualFrameworkPath);

            Directory.Delete(root, true);
        }

        [Test]
        public void GetFrameworkPath_PathDoesNotExist_ReturnsEmpty()
        {
            var actualFrameworkPath = SentryXcodeProject.GetFrameworkPath("Temp");

            Assert.AreEqual(string.Empty, actualFrameworkPath);
        }

        [Test]
        public void AddSentryToFramework_NewXcodeProject_SentryWasAdded()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectPath = Path.Combine(assemblyPath, "TestFiles");
            var xcodeProject = new SentryXcodeProject(projectPath, new NativeMainFixture(), new NativeOptions());

            xcodeProject.AddSentryFramework();

            var projectAsString = xcodeProject.ProjectToString();
            StringAssert.Contains("Sentry.framework in Embed Frameworks", projectAsString);
            StringAssert.Contains("$(PROJECT_DIR)/Frameworks/io.sentry.unity", projectAsString);
            StringAssert.Contains(@"OTHER_LDFLAGS = ""-ObjC"";", projectAsString);
        }

        [Test]
        public void CreateNativeOptions_NewXcodeProject_NativeOptionsAdded()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectPath = Path.Combine(assemblyPath, "TestFiles");
            var xcodeProject = new SentryXcodeProject(projectPath, new NativeMainFixture(), new NativeOptions());

            xcodeProject.AddNativeOptions(new SentryOptions());

            var projectAsString = xcodeProject.ProjectToString();
            StringAssert.Contains("SentryOptions.m", projectAsString);
        }
    }
}
