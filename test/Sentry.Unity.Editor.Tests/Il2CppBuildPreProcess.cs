using System;
using NUnit.Framework;

namespace Sentry.Unity.Editor.Tests;

public class Il2CppBuildPreProcessTests
{
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
}
