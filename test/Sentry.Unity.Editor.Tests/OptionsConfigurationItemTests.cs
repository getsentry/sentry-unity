using NUnit.Framework;
using Sentry.Unity.Editor.ConfigurationWindow;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Tests;

public class SetScriptTestOptionsConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options) { }
}

public class SetScriptTestCliOptionsConfiguration : SentryCliOptionsConfiguration
{
    public override void Configure(SentryCliOptions cliOptions) { }
}

public class OptionsConfigurationItemTests
{
    private ScriptableSentryUnityOptions _options = null!;
    private SentryCliOptions _cliOptions = null!;

    [SetUp]
    public void Setup()
    {
        _options = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
        _cliOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(SentryOptionsConfiguration.GetAssetPath(nameof(SetScriptTestOptionsConfiguration)));
        AssetDatabase.DeleteAsset(SentryCliOptionsConfiguration.GetAssetPath(nameof(SetScriptTestCliOptionsConfiguration)));
        Object.DestroyImmediate(_options);
        Object.DestroyImmediate(_cliOptions);
    }

    [Test]
    public void SetScript_CalledTwice_KeepsReferenceToOptionsConfigurationAsset()
    {
        OptionsConfigurationItem.SetScript(nameof(SetScriptTestOptionsConfiguration), _options, _cliOptions);
        OptionsConfigurationItem.SetScript(nameof(SetScriptTestOptionsConfiguration), _options, _cliOptions);

        var asset = AssetDatabase.LoadAssetAtPath<SentryOptionsConfiguration>(SentryOptionsConfiguration.GetAssetPath(nameof(SetScriptTestOptionsConfiguration)));
        Assert.IsTrue(asset != null);
        Assert.AreSame(asset, _options.OptionsConfiguration);
    }

    [Test]
    public void SetScript_CalledTwice_KeepsReferenceToCliOptionsConfigurationAsset()
    {
        OptionsConfigurationItem.SetScript(nameof(SetScriptTestCliOptionsConfiguration), _options, _cliOptions);
        OptionsConfigurationItem.SetScript(nameof(SetScriptTestCliOptionsConfiguration), _options, _cliOptions);

        var asset = AssetDatabase.LoadAssetAtPath<SentryCliOptionsConfiguration>(SentryCliOptionsConfiguration.GetAssetPath(nameof(SetScriptTestCliOptionsConfiguration)));
        Assert.IsTrue(asset != null);
        Assert.AreSame(asset, _cliOptions.CliOptionsConfiguration);
    }
}
