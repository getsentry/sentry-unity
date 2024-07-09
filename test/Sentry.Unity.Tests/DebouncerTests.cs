using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

/// <summary>
/// Testing debouncer in realtime.
/// </summary>
public sealed class DebouncerTests
{
    private readonly TimeSpan DefaultOffset = TimeSpan.FromMilliseconds(100);

    [UnityTest]
    public IEnumerator LogTimeDebounce()
    {
        Assert.Inconclusive("Flaky"); // Ignoring because of flakiness: https://github.com/getsentry/sentry-unity/issues/335
        yield return AssertDefaultDebounce(new LogTimeDebounce(DefaultOffset));
    }

    [UnityTest]
    public IEnumerator ErrorTimeDebounce()
    {
        Assert.Inconclusive("Flaky"); // Ignoring because of flakiness: https://github.com/getsentry/sentry-unity/issues/335
        yield return AssertDefaultDebounce(new ErrorTimeDebounce(DefaultOffset));
    }

    [UnityTest]
    public IEnumerator WarningTimeDebounce()
    {
        Assert.Inconclusive("Flaky"); // Ignoring because of flakiness: https://github.com/getsentry/sentry-unity/issues/335
        yield return AssertDefaultDebounce(new WarningTimeDebounce(DefaultOffset));
    }

    private IEnumerator AssertDefaultDebounce(TimeDebounceBase debouncer)
    {
        // pass
        Assert.IsTrue(debouncer.Debounced());

        yield return new WaitForSeconds(0.050f);

        // skip
        Assert.IsFalse(debouncer.Debounced());

        yield return new WaitForSeconds(0.02f);

        // skip
        Assert.IsFalse(debouncer.Debounced());

        yield return new WaitForSeconds(0.04f);

        // pass
        Assert.IsTrue(debouncer.Debounced());
    }
}