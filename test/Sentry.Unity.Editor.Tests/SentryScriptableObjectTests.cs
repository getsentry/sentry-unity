using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Tests;

public class SentryScriptableObjectTests
{
    private string _assetPath = null!;
    private string _filePath = null!;

    [SetUp]
    public void Setup()
    {
        var directoryName = Guid.NewGuid().ToString("N");
        _assetPath = Path.Combine("Assets", directoryName, "TestOptions.asset");
        _filePath = Path.Combine(Application.dataPath, directoryName, "TestOptions.asset");
    }

    [TearDown]
    public void TearDown() => AssetDatabase.DeleteAsset(Path.GetDirectoryName(_assetPath));

    [Test]
    public void CreateOrLoad_ScriptableSentryUnityOptionsAssetDoesNotExist_CreatesNewOptionsAsset()
    {
        Assert.IsFalse(File.Exists(_filePath)); // Sanity check

        SentryScriptableObject.CreateOrLoad<ScriptableSentryUnityOptions>(_assetPath);

        Assert.IsTrue(File.Exists(_filePath));
    }

    [Test]
    public void CreateOrLoad_SentryCliOptionsAssetDoesNotExist_CreatesNewOptionsAsset()
    {
        Assert.IsFalse(File.Exists(_filePath)); // Sanity check

        SentryScriptableObject.CreateOrLoad<SentryCliOptions>(_assetPath);

        Assert.IsTrue(File.Exists(_filePath));
    }

    [Test]
    public void Load_OptionsAssetDoesNotExist_ReturnsNull()
    {
        Assert.IsFalse(File.Exists(_filePath)); // Sanity check

        var options = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(_assetPath);

        Assert.IsNull(options);
    }

    [Test]
    public void Load_ScriptableSentryUnityOptionsExist_LoadsSavedOptionsAsset()
    {
        var expectedDsn = "test_dsn";
        var options = SentryScriptableObject.CreateOrLoad<ScriptableSentryUnityOptions>(_assetPath);
        options.Dsn = expectedDsn;
        AssetDatabase.SaveAssets(); // Saving to disk

        Assert.IsTrue(File.Exists(_filePath)); // Sanity check

        var actualOptions = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(_assetPath);

        Assert.NotNull(actualOptions);
        Assert.AreEqual(expectedDsn, actualOptions!.Dsn);
    }

    [Test]
    public void Load_SentryCliOptionsExist_LoadsSavedOptionsAsset()
    {
        var expectedAuth = "test_auth";
        var options = SentryScriptableObject.CreateOrLoad<SentryCliOptions>(_assetPath);
        options.Auth = expectedAuth;
        AssetDatabase.SaveAssets(); // Saving to disk

        Assert.IsTrue(File.Exists(_filePath)); // Sanity check

        var actualOptions = SentryScriptableObject.Load<SentryCliOptions>(_assetPath);

        Assert.NotNull(actualOptions);
        Assert.AreEqual(expectedAuth, actualOptions!.Auth);
    }

    [Test]
    public void LoadCliOptions_ConfigurationSetsAuth_DoesNotChangeSavedAsset()
    {
        var savedOptions = SentryScriptableObject.CreateOrLoad<SentryCliOptions>(_assetPath);
        savedOptions.CliOptionsConfiguration = ScriptableObject.CreateInstance<AuthFromCodeConfiguration>();

        var cliOptions = SentryScriptableObject.LoadCliOptions(_assetPath);

        Assert.NotNull(cliOptions);
        Assert.AreEqual(AuthFromCodeConfiguration.Auth, cliOptions!.Auth);
        Assert.IsNull(savedOptions.Auth);
        Assert.IsNull(SentryScriptableObject.Load<SentryCliOptions>(_assetPath)!.Auth);
    }

    [Test]
    public void LoadCliOptions_OptionsAssetDoesNotExist_ReturnsNull()
    {
        Assert.IsNull(SentryScriptableObject.LoadCliOptions(_assetPath));
    }

    private class AuthFromCodeConfiguration : SentryCliOptionsConfiguration
    {
        public const string Auth = "auth_from_code";

        public override void Configure(SentryCliOptions cliOptions) => cliOptions.Auth = Auth;
    }
}
