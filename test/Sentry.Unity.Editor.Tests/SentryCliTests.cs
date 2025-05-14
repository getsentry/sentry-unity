using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.Editor.Tests;

public class SentryCliTests
{
    [Test]
    public void GetSentryCliPlatformExecutable_UnrecognizedPlatform_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => SentryCli.GetSentryCliPlatformExecutable(RuntimePlatform.LinuxPlayer));
    }

    [Test]
    [TestCase(RuntimePlatform.WindowsEditor, SentryCli.SentryCliWindows)]
    [TestCase(RuntimePlatform.OSXEditor, SentryCli.SentryCliMacOS)]
    [TestCase(RuntimePlatform.LinuxEditor, SentryCli.SentryCliLinux)]
    public void GetSentryPlatformName_RecognizedPlatform_SetsSentryCliName(RuntimePlatform platform, string expectedName)
    {
        var actualName = SentryCli.GetSentryCliPlatformExecutable(platform);

        Assert.AreEqual(expectedName, actualName);
    }

    [Test]
    public void GetSentryCliPath_InvalidFileName_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => SentryCli.GetSentryCliPath("InvalidName"));
    }

    [Test]
    public void GetSentryCliPath_ValidFileName_ReturnsPath()
    {
        var sentryCliPlatformName = SentryCli.GetSentryCliPlatformExecutable(Application.platform);
        var expectedPath = Path.GetFullPath(
            Path.Combine("Packages", SentryPackageInfo.GetName(), "Editor", "sentry-cli", sentryCliPlatformName));

        var actualPath = SentryCli.GetSentryCliPath(sentryCliPlatformName);

        Assert.AreEqual(expectedPath, actualPath);
    }

    [Test]
    public void SetExecutePermission_FileDoesNotExist_ThrowsUnauthorizedAccessException()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            Assert.Inconclusive("Skipping chmod on Windows.");
            return;
        }

        Assert.Throws<UnauthorizedAccessException>(() => SentryCli.SetExecutePermission("non-existent-file"));
    }

    [Test]
    [TestCase("")]
    [TestCase("urlOverride")]
    public void CreateSentryProperties_PropertyFileCreatedAndContainsSentryCliOptions(string urlOverride)
    {
        var propertiesDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(propertiesDirectory);

        var sentryCliTestOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
        sentryCliTestOptions.Auth = Guid.NewGuid().ToString();
        sentryCliTestOptions.Project = Guid.NewGuid().ToString();
        sentryCliTestOptions.UrlOverride = urlOverride;

        SentryCli.CreateSentryProperties(propertiesDirectory, sentryCliTestOptions, new());

        var properties = File.ReadAllText(Path.Combine(propertiesDirectory, "sentry.properties"));

        StringAssert.Contains(sentryCliTestOptions.Auth, properties);
        StringAssert.Contains(sentryCliTestOptions.Project, properties);

        if (!string.IsNullOrEmpty(sentryCliTestOptions.UrlOverride))
        {
            StringAssert.Contains(urlOverride, properties);
        }

        Directory.Delete(propertiesDirectory, true);
    }

    [Test]
    [TestCase("")]
    [TestCase("testorg")]
    public void CreateSentryProperties_OrgProvided_PropertyFileCreatedAndContainsOrg(string org)
    {
        var propertiesDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(propertiesDirectory);

        var sentryCliTestOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
        sentryCliTestOptions.Organization = org;

        SentryCli.CreateSentryProperties(propertiesDirectory, sentryCliTestOptions, new());

        var properties = File.ReadAllText(Path.Combine(propertiesDirectory, "sentry.properties"));

        if (!string.IsNullOrEmpty(sentryCliTestOptions.Organization))
        {
            StringAssert.Contains(org, properties);
        }
        else
        {
            StringAssert.DoesNotContain("defaults.org=", properties);
        }

        Directory.Delete(propertiesDirectory, true);
    }

    [Test]
    public void SetupSentryCli_WithoutArgs_ReturnsValidCliPath()
    {
        var returnedPath = SentryCli.SetupSentryCli();
        Assert.IsTrue(File.Exists(returnedPath));
    }

    [Test]
    public void SetupSentryCli_WithCustomBuildHost_ReturnsValidCliPath()
    {
        var returnedPath = SentryCli.SetupSentryCli(null, RuntimePlatform.OSXEditor);
        var expectedExeName = SentryCli.GetSentryCliPlatformExecutable(RuntimePlatform.OSXEditor);

        Assert.IsTrue(File.Exists(returnedPath));
        Assert.AreEqual(expectedExeName, Path.GetFileName(returnedPath));
    }

    [Test]
    public void SetupSentryCli_ProjectPathDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        Assert.Throws<DirectoryNotFoundException>(() => SentryCli.SetupSentryCli("non-existent-path"));
    }

    [Test]
    public void SetupSentryCli_ProjectPathExists_CopiesSentryCli()
    {
        var fakeProjectDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(fakeProjectDirectory);

        var returnedPath = SentryCli.SetupSentryCli(fakeProjectDirectory);
        var expectedPath = Path.Combine(fakeProjectDirectory, SentryCli.GetSentryCliPlatformExecutable(Application.platform));

        Assert.AreEqual(expectedPath, returnedPath);
        Assert.IsTrue(File.Exists(returnedPath));

        Directory.Delete(fakeProjectDirectory, true);
    }

    [Test]
    public void UrlOverride()
    {
        Assert.IsNull(SentryCli.UrlOverride(null, null));
        Assert.IsNull(SentryCli.UrlOverride("", null));
        Assert.IsNull(SentryCli.UrlOverride(null, ""));
        Assert.IsNull(SentryCli.UrlOverride("", ""));
        Assert.IsNull(SentryCli.UrlOverride("https://key@o447951.ingest.sentry.io/5439417", null));
        Assert.IsNull(SentryCli.UrlOverride("https://foo.sentry.io/5439417", null));
        Assert.IsNull(SentryCli.UrlOverride("http://sentry.io", null));
        Assert.AreEqual("http://127.0.0.1:8000", SentryCli.UrlOverride("http://key@127.0.0.1:8000/12345", null));
        Assert.AreEqual("pass-through", SentryCli.UrlOverride("http://key@127.0.0.1:8000/12345", "pass-through"));
        Assert.AreEqual("https://example.com", SentryCli.UrlOverride("https://key@example.com/12345", null));
        Assert.AreEqual("http://localhost:8000", SentryCli.UrlOverride("http://key@localhost:8000/12345", null));
    }
}
