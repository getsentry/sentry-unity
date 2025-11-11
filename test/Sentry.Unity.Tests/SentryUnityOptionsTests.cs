using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public sealed class SentryUnityOptionsTests
{
    class Fixture
    {
        public TestApplication Application { get; set; } = new(
            productName: "TestApplication",
            version: "0.1.0",
            buildGUID: "12345",
            persistentDataPath: "test/persistent/data/path");
        public bool IsBuilding { get; set; }

        public SentryUnityOptions GetSut() => new(Application, isBuilding: IsBuilding);
    }

    [SetUp]
    public void Setup() => _fixture = new Fixture();
    private Fixture _fixture = null!;

    [Test]
    [TestCase(true, true, "production")]
    [TestCase(true, false, "editor")]
    [TestCase(false, false, "production")]
    [TestCase(false, true, "production")]
    public void Ctor_Environment_IsNull(bool isEditor, bool isBuilding, string expectedEnvironment)
    {
        _fixture.Application = new TestApplication(isEditor: isEditor);
        _fixture.IsBuilding = isBuilding;

        var sut = _fixture.GetSut();

        Assert.AreEqual(expectedEnvironment, sut.Environment);
    }

    [Test]
    [TestCase(true, "some/path", "some/path")]
    [TestCase(false, "some/path", null)]
    public void Ctor_IfPlatformIsKnown_SetsCacheDirectoryPath(bool isKnownPlatform, string applicationDataPath, string? expectedCacheDirectoryPath)
    {
        // Picking obsolete CloudRendering because it won't accidentally be supported
#pragma warning disable CS0618 // CloudRendering obsolete
        _fixture.Application.Platform = isKnownPlatform ? RuntimePlatform.LinuxPlayer : RuntimePlatform.CloudRendering;
#pragma warning restore CS0618 // CloudRendering is obsolete
        _fixture.Application.PersistentDataPath = applicationDataPath;

        var sut = _fixture.GetSut();

        Assert.AreEqual(sut.CacheDirectoryPath, expectedCacheDirectoryPath);
    }

    [Test]
    public void Ctor_IsGlobalModeEnabled_IsTrue() => Assert.IsTrue(_fixture.GetSut().IsGlobalModeEnabled);

    [Test]
    public void Ctor_Release_IsProductNameAtVersion() =>
        Assert.AreEqual(
            $"{_fixture.Application.ProductName}@{_fixture.Application.Version}",
            _fixture.GetSut().Release);

    [Test]
    [TestCase("\n")]
    [TestCase("\t")]
    [TestCase("/")]
    [TestCase("\\")]
    [TestCase("..")]
    [TestCase("@")]
    public void Ctor_Release_DoesNotContainInvalidCharacters(string invalidString)
    {
        var prefix = "test";
        var suffix = "application";
        var version = "0.1.0";
        _fixture.Application = new TestApplication(productName: $"{prefix}{invalidString}{suffix}", version: version);

        Assert.AreEqual($"{prefix}_{suffix}@{version}", _fixture.GetSut().Release);
    }

    [Test]
    public void Ctor_Release_IgnoresDotOnlyProductNames()
    {
        _fixture.Application = new TestApplication(productName: ".........", version: "0.1.0");
        Assert.AreEqual($"{_fixture.Application.Version}", _fixture.GetSut().Release);
    }

    [Test]
    [TestCase(RuntimePlatform.WindowsPlayer, true)]
    [TestCase(RuntimePlatform.WindowsServer, true)]
    [TestCase(RuntimePlatform.OSXPlayer, true)]
    [TestCase(RuntimePlatform.OSXServer, true)]
    [TestCase(RuntimePlatform.LinuxPlayer, true)]
    [TestCase(RuntimePlatform.LinuxServer, true)]
    [TestCase(RuntimePlatform.Android, false)]
    [TestCase(RuntimePlatform.IPhonePlayer, false)]
    [TestCase(RuntimePlatform.GameCoreXboxSeries, false)]
    [TestCase(RuntimePlatform.GameCoreXboxOne, false)]
    [TestCase(RuntimePlatform.PS4, false)]
    [TestCase(RuntimePlatform.PS5, false)]
    [TestCase(RuntimePlatform.Switch, false)]
    [TestCase(RuntimePlatform.WindowsEditor, false)]
    [TestCase(RuntimePlatform.OSXEditor, false)]
    [TestCase(RuntimePlatform.LinuxEditor, false)]
    [TestCase(RuntimePlatform.WebGLPlayer, false)]
    public void Ctor_IsEnvironmentUser_DefaultsBasedOnPlatform(RuntimePlatform platform, bool expectedValue)
    {
        _fixture.Application.Platform = platform;

        var sut = _fixture.GetSut();

        Assert.AreEqual(expectedValue, sut.IsEnvironmentUser);
    }
}
