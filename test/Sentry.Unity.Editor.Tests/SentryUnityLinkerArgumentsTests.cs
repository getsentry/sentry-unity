using NUnit.Framework;

namespace Sentry.Unity.Editor.Tests;

public class SentryUnityLinkerArgumentsTests
{
    [Test]
    public void AddLinkSymbolsArgument_NoExistingArguments_AddsArgument()
    {
        string? arguments = null;

        SentryUnityLinkerArguments.AddLinkSymbolsArgument(() => arguments, s => arguments = s);

        Assert.That(arguments, Is.EqualTo(SentryUnityLinkerArguments.LinkSymbolsArgument));
    }

    [Test]
    public void AddLinkSymbolsArgument_ExistingArguments_PreservesExistingArguments()
    {
        string? arguments = "--MyArgument";

        SentryUnityLinkerArguments.AddLinkSymbolsArgument(() => arguments, s => arguments = s);

        Assert.That(arguments, Is.EqualTo($"--MyArgument {SentryUnityLinkerArguments.LinkSymbolsArgument}"));
    }

    [Test]
    public void AddLinkSymbolsArgument_ArgumentAlreadyAdded_AddsArgumentOnlyOnce()
    {
        string? arguments = $"--MyArgument {SentryUnityLinkerArguments.LinkSymbolsArgument}";

        SentryUnityLinkerArguments.AddLinkSymbolsArgument(() => arguments, s => arguments = s);

        Assert.That(arguments, Is.EqualTo($"--MyArgument {SentryUnityLinkerArguments.LinkSymbolsArgument}"));
    }
}
