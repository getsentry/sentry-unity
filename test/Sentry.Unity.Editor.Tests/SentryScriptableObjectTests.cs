using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Sentry.Unity.Editor.Tests;

public class SentryScriptableObjectTests
{
    private string _tempPath = null!; // Assigned in Setup

    [SetUp]
    public void Setup() => _tempPath = Path.Combine("Assets", Guid.NewGuid().ToString(), "TestOptions.asset");

    [TearDown]
    public void TearDown() => AssetDatabase.DeleteAsset(Path.GetDirectoryName(_tempPath));

    [Test]
    public void CreateOrLoad_ScriptableSentryUnityOptionsAssetDoesNotExist_CreatesNewOptionsAsset()
    {
        Assert.IsFalse(File.Exists(_tempPath)); // Sanity check

        SentryScriptableObject.CreateOrLoad<ScriptableSentryUnityOptions>(_tempPath);

        Assert.IsTrue(File.Exists(_tempPath));
    }

    [Test]
    public void CreateOrLoad_SentryCliOptionsAssetDoesNotExist_CreatesNewOptionsAsset()
    {
        Assert.IsFalse(File.Exists(_tempPath)); // Sanity check

        SentryScriptableObject.CreateOrLoad<SentryCliOptions>(_tempPath);

        Assert.IsTrue(File.Exists(_tempPath));
    }

    [Test]
    public void Load_OptionsAssetDoesNotExist_ReturnsNull()
    {
        Assert.IsFalse(File.Exists(_tempPath)); // Sanity check

        var options = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(_tempPath);

        Assert.IsNull(options);
    }

    [Test]
    public void Load_ScriptableSentryUnityOptionsExist_LoadsSavedOptionsAsset()
    {
        var expectedDsn = "test_dsn";
        var options = SentryScriptableObject.CreateOrLoad<ScriptableSentryUnityOptions>(_tempPath);
        options.Dsn = expectedDsn;
        AssetDatabase.SaveAssets(); // Saving to disk

        Assert.IsTrue(File.Exists(_tempPath)); // Sanity check

        var actualOptions = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(_tempPath);

        Assert.NotNull(actualOptions);
        Assert.AreEqual(expectedDsn, actualOptions!.Dsn);
    }

    [Test]
    public void Load_SentryCliOptionsExist_LoadsSavedOptionsAsset()
    {
        var expectedAuth = "test_auth";
        var options = SentryScriptableObject.CreateOrLoad<SentryCliOptions>(_tempPath);
        options.Auth = expectedAuth;
        AssetDatabase.SaveAssets(); // Saving to disk

        Assert.IsTrue(File.Exists(_tempPath)); // Sanity check

        var actualOptions = SentryScriptableObject.Load<SentryCliOptions>(_tempPath);

        Assert.NotNull(actualOptions);
        Assert.AreEqual(expectedAuth, actualOptions!.Auth);
    }
}