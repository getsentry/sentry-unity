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

    [Test]
    public void RemoveLinkSymbolsArgument_OnlyArgument_RemovesArgument()
    {
        string? unityLinkerArguments = SentryUnityLinkerProcessor.LinkSymbolsArgument;

        SentryUnityLinkerProcessor.RemoveLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);

        Assert.That(unityLinkerArguments, Is.Empty);
    }

    [Test]
    public void RemoveLinkSymbolsArgument_ExistingArguments_PreservesExistingArguments()
    {
        string? unityLinkerArguments = $"--existing-argument {SentryUnityLinkerProcessor.LinkSymbolsArgument}";

        SentryUnityLinkerProcessor.RemoveLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);

        Assert.That(unityLinkerArguments, Is.EqualTo("--existing-argument"));
    }

    [Test]
    public void RemoveLinkSymbolsArgument_ArgumentNotPresent_DoesNotModifyArguments()
    {
        string? unityLinkerArguments = "--existing-argument";

        SentryUnityLinkerProcessor.RemoveLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);

        Assert.That(unityLinkerArguments, Is.EqualTo("--existing-argument"));
    }

    [Test]
    public void RemoveLinkSymbolsArgument_ArgumentPreviouslyAdded_RestoresOriginalArguments()
    {
        string? unityLinkerArguments = "--existing-argument";

        SentryUnityLinkerProcessor.AddLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);
        SentryUnityLinkerProcessor.RemoveLinkSymbolsArgument(
            () => unityLinkerArguments,
            arguments => unityLinkerArguments = arguments);

        Assert.That(unityLinkerArguments, Is.EqualTo("--existing-argument"));
    }
}
