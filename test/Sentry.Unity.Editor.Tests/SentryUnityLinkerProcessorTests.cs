using NUnit.Framework;

namespace Sentry.Unity.Editor.Tests;

public class SentryUnityLinkerProcessorTests
{
    [Test]
    public void AddLinkSymbolsArgument_NoExistingArguments_AddsArgument()
    {
        string? unityLinkerArguments = null;

        SentryUnityLinkerProcessor.AddLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);

        Assert.That(unityLinkerArguments, Is.EqualTo(SentryUnityLinkerProcessor.LinkSymbolsArgument));
    }

    [Test]
    public void AddLinkSymbolsArgument_ExistingArguments_PreservesExistingArguments()
    {
        string? unityLinkerArguments = "--existing-argument";

        SentryUnityLinkerProcessor.AddLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);

        Assert.That(unityLinkerArguments, Is.EqualTo($"--existing-argument {SentryUnityLinkerProcessor.LinkSymbolsArgument}"));
    }

    [Test]
    public void AddLinkSymbolsArgument_ArgumentAlreadyAdded_AddsArgumentOnlyOnce()
    {
        string? unityLinkerArguments = $"--existing-argument {SentryUnityLinkerProcessor.LinkSymbolsArgument}";

        SentryUnityLinkerProcessor.AddLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);

        Assert.That(unityLinkerArguments, Is.EqualTo($"--existing-argument {SentryUnityLinkerProcessor.LinkSymbolsArgument}"));
    }
}
