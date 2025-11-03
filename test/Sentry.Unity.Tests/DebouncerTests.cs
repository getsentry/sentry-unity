using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

/// <summary>
/// Testing content-based debouncer.
/// </summary>
public sealed class DebouncerTests
{
    private readonly TimeSpan DefaultWindow = TimeSpan.FromMilliseconds(500);

    [Test]
    public void ContentDebounce_DifferentMessages_AllowsThrough()
    {
        var debouncer = new ContentDebounce(DefaultWindow);

        // First message should pass
        Assert.IsTrue(debouncer.Debounced("Error message 1", "Stacktrace 1", LogType.Error));

        // Different message should pass immediately
        Assert.IsTrue(debouncer.Debounced("Error message 2", "Stacktrace 2", LogType.Error));

        // Another different message should pass
        Assert.IsTrue(debouncer.Debounced("Error message 3", "Stacktrace 3", LogType.Error));
    }

    [Test]
    public void ContentDebounce_SameMessage_BlocksDuplicate()
    {
        var debouncer = new ContentDebounce(DefaultWindow);

        // First message should pass
        Assert.IsTrue(debouncer.Debounced("Error message", "Stacktrace", LogType.Error));

        // Same message should be blocked
        Assert.IsFalse(debouncer.Debounced("Error message", "Stacktrace", LogType.Error));

        // Still blocked
        Assert.IsFalse(debouncer.Debounced("Error message", "Stacktrace", LogType.Error));
    }

    [UnityTest]
    public IEnumerator ContentDebounce_SameMessage_AllowsAfterWindow()
    {
        var debouncer = new ContentDebounce(TimeSpan.FromMilliseconds(100));

        // First message should pass
        Assert.IsTrue(debouncer.Debounced("Error message", "Stacktrace", LogType.Error));

        // Same message immediately blocked
        Assert.IsFalse(debouncer.Debounced("Error message", "Stacktrace", LogType.Error));

        // Wait for debounce window to expire
        yield return new WaitForSeconds(0.15f);

        // Same message should now pass
        Assert.IsTrue(debouncer.Debounced("Error message", "Stacktrace", LogType.Error));
    }

    [Test]
    public void ContentDebounce_DifferentLogTypes_AllowsThrough()
    {
        var debouncer = new ContentDebounce(DefaultWindow);

        Assert.IsTrue(debouncer.Debounced("Message", "Stack", LogType.Error));
        Assert.IsTrue(debouncer.Debounced("Message", "Stack", LogType.Warning));
        Assert.IsTrue(debouncer.Debounced("Message", "Stack", LogType.Log));
    }

    [Test]
    public void ContentDebounce_SameMessageDifferentStacktrace_Deduplicates()
    {
        var debouncer = new ContentDebounce(DefaultWindow);

        // First occurrence
        Assert.IsTrue(debouncer.Debounced("Error occurred", "at Main() line 10", LogType.Error));

        // Same message, slightly different stacktrace (but same first line)
        Assert.IsFalse(debouncer.Debounced("Error occurred", "at Main() line 10\nat Foo()", LogType.Error));
    }

    [Test]
    public void ContentDebounce_DifferentFirstLineOfStacktrace_AllowsThrough()
    {
        var debouncer = new ContentDebounce(DefaultWindow);

        // First occurrence
        Assert.IsTrue(debouncer.Debounced("Error occurred", "at Main() line 10", LogType.Error));

        // Same message but different location (different first line of stacktrace)
        Assert.IsTrue(debouncer.Debounced("Error occurred", "at Other() line 5", LogType.Error));
    }

    [Test]
    public void ContentDebounce_ManyDifferentMessages_HandlesCorrectly()
    {
        var debouncer = new ContentDebounce(DefaultWindow);

        // Add many different messages
        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(debouncer.Debounced($"Error {i}", $"Stack {i}", LogType.Error));
        }

        // Duplicates of first messages should still be blocked
        Assert.IsFalse(debouncer.Debounced("Error 0", "Stack 0", LogType.Error));
        Assert.IsFalse(debouncer.Debounced("Error 50", "Stack 50", LogType.Error));
    }
}
