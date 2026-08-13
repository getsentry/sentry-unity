using System;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Editor.Tests;

public class Il2CppBuildPreProcessTests
{
    private static readonly TestApplication SupportedUnity = new(unityVersion: "6000.5.0f1");

    private string arguments = null!;
    private string resultingArguments = null!;

    [SetUp]
    public void Setup()
    {
        arguments = string.Empty;
        resultingArguments = string.Empty;
    }

    [Test]
    public void SetAdditionalArguments_Il2CppEnabled_AddsArguments()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = true };

        Il2CppBuildPreProcess.SetAdditionalIl2CppArguments(options, () => arguments, s => resultingArguments = s);

        Assert.That(resultingArguments, Does.Contain(Il2CppBuildPreProcess.SourceMappingArgument));
    }

    [Test]
    public void SetAdditionalArguments_Il2CppDisabled_FoesNotAddArguments()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = false };

        Il2CppBuildPreProcess.SetAdditionalIl2CppArguments(options, () => arguments, s => resultingArguments = s);

        Assert.That(resultingArguments, Does.Not.Contain(Il2CppBuildPreProcess.SourceMappingArgument));
    }

    [Test]
    public void SetAdditionalArguments_Il2CppEnabled_ExistingArgumentsDoNotGetOverwritten()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = true };
        var expectedArgument = "--MyArgument";
        arguments = expectedArgument;

        Il2CppBuildPreProcess.SetAdditionalIl2CppArguments(options, () => arguments, s => resultingArguments = s);

        Assert.That(resultingArguments, Does.Contain(Il2CppBuildPreProcess.SourceMappingArgument)); // sanity check
        Assert.That(resultingArguments, Does.Contain(expectedArgument));
    }

    [Test]
    public void SetAdditionalArguments_Il2CppEnabledAndArgumentAlreadyAdded_AddsArgumentsOnlyOnce()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = true };

        Il2CppBuildPreProcess.SetAdditionalIl2CppArguments(options, () => arguments, s => resultingArguments = s);
        Assert.That(resultingArguments, Does.Contain(Il2CppBuildPreProcess.SourceMappingArgument)); // sanity check

        Il2CppBuildPreProcess.SetAdditionalIl2CppArguments(options, () => arguments, s => resultingArguments = s);

        Assert.That(resultingArguments, Does.Contain(Il2CppBuildPreProcess.SourceMappingArgument)); // sanity check
        var occurrences = 0;
        var i = 0;
        while ((i = resultingArguments.IndexOf(Il2CppBuildPreProcess.SourceMappingArgument, i, StringComparison.Ordinal)) != -1)
        {
            i += Il2CppBuildPreProcess.SourceMappingArgument.Length;
            occurrences++;
        }

        Assert.AreEqual(1, occurrences);
    }

    [Test]
    public void SetAdditionalArguments_Il2CppDisabledAndArgumentAlreadyAdded_RemovesArguments()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = false };
        var expectedArgument = "--MyArgument";
        arguments = $"{expectedArgument} {Il2CppBuildPreProcess.SourceMappingArgument}";

        Il2CppBuildPreProcess.SetAdditionalIl2CppArguments(options, () => arguments, s => resultingArguments = s);

        Assert.That(resultingArguments, Does.Contain(expectedArgument));
        Assert.That(resultingArguments, Does.Not.Contain(Il2CppBuildPreProcess.SourceMappingArgument));
    }

    [Test]
    public void SetAdditionalUnityLinkerArguments_Il2CppEnabled_AddsArgument()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = true };

        Il2CppBuildPreProcess.SetAdditionalUnityLinkerArguments(options, () => null, s => resultingArguments = s, SupportedUnity);

        Assert.That(resultingArguments, Is.EqualTo(Il2CppBuildPreProcess.LinkSymbolsArgument));
    }

    [Test]
    public void SetAdditionalUnityLinkerArguments_Il2CppDisabled_DoesNotAddArgument()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = false };

        Il2CppBuildPreProcess.SetAdditionalUnityLinkerArguments(options, () => null, s => resultingArguments = s, SupportedUnity);

        Assert.That(resultingArguments, Does.Not.Contain(Il2CppBuildPreProcess.LinkSymbolsArgument));
    }

    [Test]
    public void SetAdditionalUnityLinkerArguments_Il2CppEnabled_ExistingArgumentsDoNotGetOverwritten()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = true };
        var expectedArgument = "--MyArgument";

        Il2CppBuildPreProcess.SetAdditionalUnityLinkerArguments(options, () => expectedArgument, s => resultingArguments = s, SupportedUnity);

        Assert.That(resultingArguments, Is.EqualTo($"{expectedArgument} {Il2CppBuildPreProcess.LinkSymbolsArgument}"));
    }

    [Test]
    public void SetAdditionalUnityLinkerArguments_Il2CppDisabledAndArgumentAlreadyAdded_RemovesArgument()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = false };
        var expectedArgument = "--MyArgument";
        arguments = $"{expectedArgument} {Il2CppBuildPreProcess.LinkSymbolsArgument}";

        Il2CppBuildPreProcess.SetAdditionalUnityLinkerArguments(options, () => arguments, s => resultingArguments = s, SupportedUnity);

        Assert.That(resultingArguments, Is.EqualTo(expectedArgument));
    }

    [Test]
    public void SetAdditionalUnityLinkerArguments_ArgumentAlreadyAdded_AddsArgumentOnlyOnce()
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = true };
        arguments = $"--MyArgument {Il2CppBuildPreProcess.LinkSymbolsArgument}";

        Il2CppBuildPreProcess.SetAdditionalUnityLinkerArguments(options, () => arguments, s => resultingArguments = s, SupportedUnity);

        Assert.That(resultingArguments, Is.Empty);
    }

    [Test]
    [TestCase("2021.3.0f1")]
    [TestCase("6000.0.0f1")]
    [TestCase("6000.4.9f1")]
    public void SetAdditionalUnityLinkerArguments_UnsupportedUnityVersion_DoesNotSetArguments(string unityVersion)
    {
        var options = new SentryUnityOptions { Il2CppLineNumberSupportEnabled = true };
        var application = new TestApplication(unityVersion: unityVersion);

        Il2CppBuildPreProcess.SetAdditionalUnityLinkerArguments(options, () => null, s => resultingArguments = s, application);

        Assert.That(resultingArguments, Is.Empty);
    }
}
