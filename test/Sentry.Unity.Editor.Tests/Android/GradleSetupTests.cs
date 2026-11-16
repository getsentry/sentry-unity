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

    private string UnityLibraryGradle => File.ReadAllText(Path.Combine(GradleProjectPath, "unityLibrary", "build.gradle"));

    [Test]
    public void UpdateGradleProject_SessionReplayEnabled_AddsReplayDependencies()
    {
        var sut = new GradleSetup(Logger, GradleProjectPath, sessionReplayEnabled: true);

        sut.UpdateGradleProject();

        StringAssert.Contains(GradleSetup.SdkDependencies + GradleSetup.ReplayDependencies, UnityLibraryGradle);
    }

    [Test]
    public void UpdateGradleProject_SessionReplayDisabled_AddsNoReplayDependencies()
    {
        var original = UnityLibraryGradle;
        var sut = new GradleSetup(Logger, GradleProjectPath, sessionReplayEnabled: false);

        sut.UpdateGradleProject();

        Assert.AreEqual(original.Replace("android {", GradleSetup.SdkDependencies + "\n}\n\nandroid {"), UnityLibraryGradle);
        StringAssert.DoesNotContain("sentry-android-replay", UnityLibraryGradle);
        StringAssert.DoesNotContain("kotlin-stdlib:1.9.24", UnityLibraryGradle);
    }

    [Test]
    public void ClearGradleProject_SessionReplayDependenciesAdded_RestoresOriginal()
    {
        var original = UnityLibraryGradle;
        new GradleSetup(Logger, GradleProjectPath, sessionReplayEnabled: true).UpdateGradleProject();

        new GradleSetup(Logger, GradleProjectPath).ClearGradleProject();

        Assert.AreEqual(original, UnityLibraryGradle);
    }

    [Test]
    public void UpdateGradleProject_SessionReplayToggled_SwapsDependencies()
    {
        var original = UnityLibraryGradle;
        new GradleSetup(Logger, GradleProjectPath, sessionReplayEnabled: true).UpdateGradleProject();

        new GradleSetup(Logger, GradleProjectPath, sessionReplayEnabled: false).UpdateGradleProject();

        StringAssert.DoesNotContain("sentry-android-replay", UnityLibraryGradle);
        StringAssert.DoesNotContain("kotlin-stdlib:1.9.24", UnityLibraryGradle);
        Assert.AreEqual(original.Replace("android {", GradleSetup.SdkDependencies + "\n}\n\nandroid {"), UnityLibraryGradle);

        new GradleSetup(Logger, GradleProjectPath, sessionReplayEnabled: true).UpdateGradleProject();

        StringAssert.Contains(GradleSetup.SdkDependencies + GradleSetup.ReplayDependencies, UnityLibraryGradle);
        Assert.AreEqual(1, UnityLibraryGradle.Split(new[] { "sentry-android-core-release" }, StringSplitOptions.None).Length - 1);
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
