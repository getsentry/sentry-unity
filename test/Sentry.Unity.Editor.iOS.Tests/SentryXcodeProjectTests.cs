using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS.Tests;

public class SentryXcodeProjectTests
{
    private class Fixture
    {
        public string ProjectRoot { get; set; } =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestFiles", "2019_4");
        public SentryUnityOptions Options { get; set; }
        public TestLogger TestLogger { get; set; }

        public Fixture()
        {
            TestLogger = new TestLogger();
            Options = new SentryUnityOptions
            {
                Debug = true,
                DiagnosticLevel = SentryLevel.Debug,
                DiagnosticLogger = TestLogger
            };
        }

        public SentryXcodeProject GetSut() => new(ProjectRoot, TestLogger);
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
    public void AddSentryFramework_CleanXcodeProject_SentryWasAdded()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        xcodeProject.AddSentryFramework();

        StringAssert.Contains(SentryXcodeProject.FrameworkName, xcodeProject.ProjectToString());
    }

    [Test]
    public void AddSentryFramework_EmbedsFramework()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        xcodeProject.AddSentryFramework();

        StringAssert.Contains("/* Sentry.xcframework in Embed Frameworks */", xcodeProject.ProjectToString());
    }

    [Test]
    public void AddSentryFramework_FrameworkSearchPathAlreadySet_DoesNotGetOverwritten()
    {
        const string testPath = "path_that_should_not_get_overwritten";
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();
        xcodeProject.SetSearchPathBuildProperty(testPath);

        xcodeProject.AddSentryFramework();

        StringAssert.Contains(testPath, xcodeProject.ProjectToString());
    }

    [Test]
    public void AddSentryFramework_BitcodeDisabled()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        xcodeProject.AddSentryFramework();

        StringAssert.Contains("ENABLE_BITCODE = NO;", xcodeProject.ProjectToString());
    }

    [Test]
    public void AddSentryNativeBridges_FrameworkSearchPathAlreadySet_DoesNotGetOverwritten()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        xcodeProject.AddSentryNativeBridge();

        StringAssert.Contains(SentryXcodeProject.BridgeName, xcodeProject.ProjectToString());
    }

    [Test]
    public void CreateNativeOptions_CleanXcodeProject_NativeOptionsAdded()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        xcodeProject.AddNativeOptions(_fixture.Options, (_, _) => { });

        StringAssert.Contains(SentryXcodeProject.OptionsName, xcodeProject.ProjectToString());
    }

    [Test]
    public void AddBuildPhaseSymbolUpload_CleanXcodeProject_BuildPhaseSymbolUploadAdded()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        var didContainUploadPhase = xcodeProject.MainTargetContainsSymbolUploadBuildPhase();
        xcodeProject.AddBuildPhaseSymbolUpload(new SentryCliOptions());
        var doesContainUploadPhase = xcodeProject.MainTargetContainsSymbolUploadBuildPhase();

        Assert.IsFalse(didContainUploadPhase);
        Assert.IsTrue(doesContainUploadPhase);
    }

    [Test]
    public void AddBuildPhaseSymbolUpload_PhaseAlreadyAdded_LogsAndDoesNotAddAgain()
    {
        const int expectedBuildPhaseOccurence = 1;
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        xcodeProject.AddBuildPhaseSymbolUpload(new SentryCliOptions());
        xcodeProject.AddBuildPhaseSymbolUpload(new SentryCliOptions());

        var actualBuildPhaseOccurence = Regex.Matches(xcodeProject.ProjectToString(),
            Regex.Escape(SentryXcodeProject.SymbolUploadPhaseName)).Count;

        Assert.IsTrue(_fixture.TestLogger.Logs.Any(log =>
            log.logLevel == SentryLevel.Info &&
            log.message.Contains("already added."))); // Sanity check
        Assert.AreEqual(expectedBuildPhaseOccurence, actualBuildPhaseOccurence);
    }

    [Test]
    public void AddBuildPhaseSymbolUpload_OptionsEnabled_SetsCorrectBuildProperties()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        var sentryCliOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
        sentryCliOptions.UploadSymbols = true;
        sentryCliOptions.UploadSources = true;
        sentryCliOptions.IgnoreCliErrors = true;

        xcodeProject.AddBuildPhaseSymbolUpload(sentryCliOptions);
        var projectString = xcodeProject.ProjectToString();

        StringAssert.Contains($"{SentryXcodeProject.SymbolUploadPropertyName} = YES", projectString);
        StringAssert.Contains($"{SentryXcodeProject.IncludeSourcesPropertyName} = YES", projectString);
        StringAssert.Contains($"{SentryXcodeProject.AllowFailurePropertyName} = YES", projectString);
        StringAssert.Contains($"{SentryXcodeProject.PrintLogsPropertyName} = NO", projectString);
    }

    [Test]
    public void AddBuildPhaseSymbolUpload_OptionsDisabled_SetsCorrectBuildProperties()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        var sentryCliOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
        sentryCliOptions.UploadSymbols = false;
        sentryCliOptions.UploadSources = false;
        sentryCliOptions.IgnoreCliErrors = false;

        xcodeProject.AddBuildPhaseSymbolUpload(sentryCliOptions);
        var projectString = xcodeProject.ProjectToString();

        StringAssert.Contains($"{SentryXcodeProject.SymbolUploadPropertyName} = NO", projectString);
        StringAssert.Contains($"{SentryXcodeProject.IncludeSourcesPropertyName} = NO", projectString);
        StringAssert.Contains($"{SentryXcodeProject.AllowFailurePropertyName} = NO", projectString);
        StringAssert.Contains($"{SentryXcodeProject.PrintLogsPropertyName} = YES", projectString);
    }

    [Test]
    public void AddBuildPhaseSymbolUpload_UploadScriptContainsPropertyReferences()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        var sentryCliOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
        xcodeProject.AddBuildPhaseSymbolUpload(sentryCliOptions);
        var projectString = xcodeProject.ProjectToString();

        StringAssert.Contains($"${{{SentryXcodeProject.SymbolUploadPropertyName}}}", projectString);
        StringAssert.Contains($"${{{SentryXcodeProject.IncludeSourcesPropertyName}}}", projectString);
        StringAssert.Contains($"${{{SentryXcodeProject.AllowFailurePropertyName}}}", projectString);
        StringAssert.Contains($"${{{SentryXcodeProject.PrintLogsPropertyName}}}", projectString);
    }

    [Test]
    public void AddBuildPhaseSymbolUpload_ScriptContainsSentryCliPath()
    {
        var xcodeProject = _fixture.GetSut();
        xcodeProject.ReadFromProjectFile();

        var sentryCliOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
        xcodeProject.AddBuildPhaseSymbolUpload(sentryCliOptions);

        StringAssert.Contains(SentryCli.SentryCliMacOS, xcodeProject.ProjectToString());
    }
}
