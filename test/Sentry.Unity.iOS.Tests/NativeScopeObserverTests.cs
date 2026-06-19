using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace Sentry.Unity.iOS.Tests;

public class IosNativeScopeObserverTests
{
    [Test]
    public void GetTimestamp_ReturnStringConformsToISO8601()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var timestampString = NativeScopeObserver.GetTimestamp(timestamp);
        var actualTimestamp = DateTimeOffset.ParseExact(timestampString, "o", CultureInfo.InvariantCulture);

        Assert.AreEqual(timestamp, actualTimestamp);
    }

    [Test]
    [TestCase(BreadcrumbLevel.Debug, 1)]
    [TestCase(BreadcrumbLevel.Info, 2)]
    [TestCase(BreadcrumbLevel.Warning, 3)]
    [TestCase(BreadcrumbLevel.Error, 4)]
    [TestCase(BreadcrumbLevel.Fatal, 5)]
    public void GetBreadcrumbLevel_TestCases(BreadcrumbLevel level, int expectedNativeLevel)
    {
        var actualLevel = NativeScopeObserver.GetBreadcrumbLevel(level);

        Assert.AreEqual(actualLevel, expectedNativeLevel);
    }

    [Test]
    public void GetBreadcrumbData_WithData_BuildsParallelArrays()
    {
        var breadcrumb = new Breadcrumb("message", "type", new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
        });

        var count = NativeScopeObserver.GetBreadcrumbData(breadcrumb, out var keys, out var values);

        Assert.AreEqual(2, count);
        Assert.IsNotNull(keys);
        Assert.IsNotNull(values);
        Assert.AreEqual("key1", keys![0]);
        Assert.AreEqual("value1", values![0]);
        Assert.AreEqual("key2", keys[1]);
        Assert.AreEqual("value2", values[1]);
    }

    [Test]
    public void GetBreadcrumbData_WithoutData_ReturnsZeroAndNullArrays()
    {
        var breadcrumb = new Breadcrumb("message", "type");

        var count = NativeScopeObserver.GetBreadcrumbData(breadcrumb, out var keys, out var values);

        Assert.AreEqual(0, count);
        Assert.IsNull(keys);
        Assert.IsNull(values);
    }
}
