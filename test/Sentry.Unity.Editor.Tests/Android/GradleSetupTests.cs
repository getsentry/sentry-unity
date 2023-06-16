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
    public class GradleSetupTests
    {
        private TestLogger Logger { get; set; } = new();
        private string FakeProjectPath { get; set; } = null!;
        private string UnityProjectPath { get; set; } = null!;
        private string GradleProjectPath { get; set; } = null!;

        [SetUp]
        public void SetUp()
        {
            FakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            UnityProjectPath = Path.Combine(FakeProjectPath, "UnityProject");
            GradleProjectPath = Path.Combine(FakeProjectPath, "GradleProject");

            DebugSymbolUploadTests.SetupFakeProject(FakeProjectPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(FakeProjectPath))
            {
                Directory.Delete(FakeProjectPath, true);
            }
        }

        [Test]
        public void LoadGradleScript_FileNotFound_ThrowsFileNotFoundException()
        {
            var brokenPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Assert.Throws<FileNotFoundException>(() => GradleSetup.LoadGradleScript(brokenPath));
        }

        [Test]
        [TestCase("2019.3")]
        [TestCase("2020.3")]
        [TestCase("2021.3")]
        public void UpdateGradleProject_2021_AndOlder_ModifiesGradleFiles(string unityVersion)
        {
            var sut = new GradleSetup(Logger, GradleProjectPath);

            sut.UpdateGradleProject(new TestApplication(unityVersion: unityVersion));

            var rootGradleFilePath = Path.Combine(GradleProjectPath, "build.gradle");
            var rootGradleContent = File.ReadAllText(rootGradleFilePath);
            StringAssert.Contains(GradleSetup.LocalRepository, rootGradleContent);

            var unityLibraryGradleFilePath = Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle");
            var unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
            StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent);
            StringAssert.Contains(GradleSetup.MavenCentralWithFilter, unityLibraryGradleContent);
        }

        [Test]
        public void UpdateGradleProject_2022_AndNewer_ModifiesGradleFiles()
        {
            var sut = new GradleSetup(Logger, GradleProjectPath);

            sut.UpdateGradleProject(new TestApplication(unityVersion: "2022.3"));

            var settingsGradleFilePath = Path.Combine(GradleProjectPath, "settings.gradle");
            var settingsGradleContent = File.ReadAllText(settingsGradleFilePath);
            StringAssert.Contains(GradleSetup.LocalRepository, settingsGradleContent);
            StringAssert.Contains(GradleSetup.MavenCentralWithFilter, settingsGradleContent);

            var unityLibraryGradleFilePath = Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle");
            var unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
            StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent);
        }

        [Test]
        [TestCase("2019.3", "build.gradle")]
        [TestCase("2020.3", "build.gradle")]
        [TestCase("2021.3", "build.gradle")]
        [TestCase("2022.3", "settings.gradle")]
        public void UpdateGradleProject_GradleAlreadyModified_LogsAndReturns(string unityVersion, string rootGradleFileName)
        {
            var sut = new GradleSetup(Logger, GradleProjectPath);
            sut.UpdateGradleProject(new TestApplication(unityVersion: unityVersion));

            sut.UpdateGradleProject(new TestApplication(unityVersion: unityVersion));

            var rootGradleFilePath = Path.Combine(GradleProjectPath, rootGradleFileName);
            var rootGradleContent = File.ReadAllText(rootGradleFilePath);
            StringAssert.Contains(GradleSetup.LocalRepository, rootGradleContent); // Sanity check

            var unityLibraryGradleFilePath = Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle");
            var unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
            StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent); // Sanity check

            Assert.IsTrue(Logger.Logs.Any(log =>
                log.logLevel == SentryLevel.Debug &&
                log.message.Contains("The gradle file has already been updated. Skipping.")));
        }

        [Test]
        [TestCase("2019.3", "build.gradle")]
        [TestCase("2020.3", "build.gradle")]
        [TestCase("2021.3", "build.gradle")]
        [TestCase("2022.3", "settings.gradle")]
        public void ClearGradleProject_GradleFilesModified_RemovesModification(string unityVersion, string rootGradleFileName)
        {
            var sut = new GradleSetup(Logger, GradleProjectPath);
            sut.UpdateGradleProject(new TestApplication(unityVersion: unityVersion));

            var rootGradleFilePath = Path.Combine(GradleProjectPath, rootGradleFileName);
            var rootGradleContent = File.ReadAllText(rootGradleFilePath);
            StringAssert.Contains(GradleSetup.LocalRepository, rootGradleContent); // Sanity check

            var unityLibraryGradleFilePath = Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle");
            var unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
            StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent); // Sanity check

            sut.ClearGradleProject(new TestApplication(unityVersion: unityVersion));

            rootGradleContent = File.ReadAllText(rootGradleFilePath);
            StringAssert.DoesNotContain(GradleSetup.LocalRepository, rootGradleContent); // Sanity check
            StringAssert.DoesNotContain(GradleSetup.MavenCentralWithFilter, rootGradleContent);

            unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
            StringAssert.DoesNotContain(GradleSetup.SdkDependencies, unityLibraryGradleContent); // Sanity check
            StringAssert.DoesNotContain(GradleSetup.MavenCentralWithFilter, unityLibraryGradleContent);
        }

        [Test]
        [TestCase("InsertIntoScope/build.gradle_test_1")]
        [TestCase("InsertIntoScope/build.gradle_test_2")]
        [TestCase("InsertIntoScope/build.gradle_test_3")]
        [TestCase("InsertIntoScope/build.gradle_test_4")]
        [TestCase("InsertIntoScope/build.gradle_test_5")]
        public void InsertIntoScope_ResultMatchesExpected(string testCaseFileName)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testCasePath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + ".txt");
            var testCaseExpectedPath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + "_expected.txt");
            var expectedGradleContent = File.ReadAllText(testCaseExpectedPath);

            var gradleContent = File.ReadAllText(testCasePath);
            var sut = new GradleSetup(Logger, GradleProjectPath);

            var actualResult = sut.InsertIntoScope(gradleContent, GradleSetup.RepositoryScopeName, GradleSetup.LocalRepository);

            StringAssert.AreEqualIgnoringCase(actualResult, expectedGradleContent);
        }

        [Test]
        [TestCase("SetMavenCentralFilter/build.gradle_test_1")]
        [TestCase("SetMavenCentralFilter/build.gradle_test_2")]
        [TestCase("SetMavenCentralFilter/build.gradle_test_3")]
        public void SetMavenCentralFilter_ResultMatchesExpected(string testCaseFileName)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testCasePath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + ".txt");
            var testCaseExpectedPath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + "_expected.txt");
            var expectedGradleContent = File.ReadAllText(testCaseExpectedPath);

            var gradleContent = File.ReadAllText(testCasePath);
            var sut = new GradleSetup(Logger, GradleProjectPath);

            var actualResult = sut.SetMavenCentralFilter(gradleContent);

            StringAssert.AreEqualIgnoringCase(actualResult, expectedGradleContent);
        }
    }
}
