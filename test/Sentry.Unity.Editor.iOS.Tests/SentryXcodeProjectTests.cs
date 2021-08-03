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
            var root = Path.GetTempPath() + Path.GetRandomFileName();
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(root, expectedFrameworkPath));
            var xcodeProject = new SentryXcodeProject("", new NativeMainFixture(), new NativeOptionsFixture());

            var actualFrameworkPath = xcodeProject.GetFrameworkPath(root);

            Assert.AreEqual(expectedFrameworkPath, actualFrameworkPath);

            Directory.Delete(root, true);
        }

        [Test]
        public void GetFrameworkPath_DevPathExists_ReturnsDevFrameworkPath()
        {
            var root = Path.GetTempPath() + Path.GetRandomFileName();
            var expectedFrameworkPath = Path.Combine("Frameworks", "io.sentry.unity.dev", "Plugins", "iOS");
            Directory.CreateDirectory(Path.Combine(root, expectedFrameworkPath));
            var xcodeProject = new SentryXcodeProject("", new NativeMainFixture(), new NativeOptionsFixture());

            var actualFrameworkPath = xcodeProject.GetFrameworkPath(root);

            Assert.AreEqual(expectedFrameworkPath, actualFrameworkPath);

            Directory.Delete(root, true);
        }

        [Test]
        public void GetFrameworkPath_PathDoesNotExist_ReturnsEmpty()
        {
            var xcodeProject = new SentryXcodeProject("", new NativeMainFixture(), new NativeOptionsFixture());

            var actualFrameworkPath = xcodeProject.GetFrameworkPath("Temp");

            Assert.AreEqual(string.Empty, actualFrameworkPath);
        }

        [Test]
        public void ValidateFramework_SentryFrameworkDoesExist_ReturnsTrue()
        {
            var root = Path.GetTempPath() + Path.GetRandomFileName();
            var frameworkPath = Path.Combine("Frameworks", "io.sentry.unity.dev", "Plugins", "iOS", "Sentry.framework");
            Directory.CreateDirectory(Path.Combine(root, frameworkPath));
            var xcodeProject = new SentryXcodeProject(root, new NativeMainFixture(), new NativeOptionsFixture());

            var valid = xcodeProject.ValidateFramework();

            Assert.IsTrue(valid);

            Directory.Delete(root, true);
        }

        [Test]
        public void ValidateFramework_SentryFrameworkDoesNotExist_ReturnsFalse()
        {
            var root = Path.GetTempPath() + Path.GetRandomFileName();
            var xcodeProject = new SentryXcodeProject(root, new NativeMainFixture(), new NativeOptionsFixture());

            var valid = xcodeProject.ValidateFramework();

            Assert.IsFalse(valid);
        }

        [Test]
        public void AddSentryFramework_NewXcodeProject_SentryWasAdded()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectPath = Path.Combine(assemblyPath, "TestFiles");
            var xcodeProject = new SentryXcodeProject(projectPath, new NativeMainFixture(), new NativeOptionsFixture());

            xcodeProject.AddSentryFramework();

            var projectAsString = xcodeProject.ProjectToString();
            StringAssert.Contains(@"OTHER_LDFLAGS = ""-ObjC"";", projectAsString);
        }

        [Test]
        public void CreateNativeOptions_NewXcodeProject_NativeOptionsAdded()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectPath = Path.Combine(assemblyPath, "TestFiles");
            var xcodeProject = new SentryXcodeProject(projectPath, new NativeMainFixture(), new NativeOptionsFixture());

            xcodeProject.AddNativeOptions(new SentryOptions());

            var projectAsString = xcodeProject.ProjectToString();
            StringAssert.Contains("SentryOptions.m", projectAsString);
        }
    }
}
