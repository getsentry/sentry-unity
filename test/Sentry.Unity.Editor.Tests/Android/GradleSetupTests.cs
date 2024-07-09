using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests.Android;

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
    public void UpdateGradleProject_ModifiesGradleFiles()
    {
        var sut = new GradleSetup(Logger, GradleProjectPath);

        sut.UpdateGradleProject();

        var unityLibraryGradleFilePath = Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle");
        var unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
        StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent);
    }

    [Test]
    public void UpdateGradleProject_GradleAlreadyModified_LogsAndReturns()
    {
        var sut = new GradleSetup(Logger, GradleProjectPath);
        sut.UpdateGradleProject();
        var unityLibraryGradleFilePath = Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle");
        var unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
        StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent); // Sanity check

        sut.UpdateGradleProject();

        unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
        StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent); // Sanity check

        Assert.IsTrue(Logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Debug &&
            log.message.Contains(GradleSetup.DependenciesAddedMessage)));
    }

    [Test]
    public void ClearGradleProject_GradleFilesModified_RemovesModification()
    {
        var sut = new GradleSetup(Logger, GradleProjectPath);
        sut.UpdateGradleProject();
        var unityLibraryGradleFilePath = Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle");
        var unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
        StringAssert.Contains(GradleSetup.SdkDependencies, unityLibraryGradleContent); // Sanity check

        sut.ClearGradleProject();

        unityLibraryGradleContent = File.ReadAllText(unityLibraryGradleFilePath);
        StringAssert.DoesNotContain(GradleSetup.SdkDependencies, unityLibraryGradleContent);
    }

    [Test]
    [TestCase("InsertIntoScope/build.gradle_test_1")]
    [TestCase("InsertIntoScope/build.gradle_test_2")]
    public void InsertIntoScope_ResultMatchesExpected(string testCaseFileName)
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var testCasePath = Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + ".txt");
        var testCaseExpectedPath =
            Path.Combine(assemblyPath, "TestFiles", "Android", testCaseFileName + "_expected.txt");
        var expectedGradleContent = File.ReadAllText(testCaseExpectedPath);

        var gradleContent = File.ReadAllText(testCasePath);
        var sut = new GradleSetup(Logger, GradleProjectPath);

        var actualResult = sut.AddSentryToGradle(gradleContent);

        StringAssert.AreEqualIgnoringCase(actualResult, expectedGradleContent);
    }
}