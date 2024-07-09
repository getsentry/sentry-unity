using System;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Editor.Tests;

public class SentryUnityVersionTests
{
    [Test]
    [TestCase("2019.4.39f1", "2019.4.39")]
    [TestCase("2021.1.1b1", "2021.1.1")]
    [TestCase("2022.1.0a17", "2022.1.0")]
    [TestCase("2022.1.0", "2022.1.0")]
    public void GetUnityVersion_WellFormedVersion_ReturnsTrimmedVersion(string unityVersion, string expectedUnityVersion)
    {
        var application = new TestApplication(unityVersion: unityVersion);
        var expectedVersion = new Version(expectedUnityVersion);

        var actualVersion = SentryUnityVersion.GetVersion(application);

        Assert.AreEqual(expectedVersion, actualVersion);
    }

    [Test]
    [TestCase("2019.4.39f1", "2019.4.39", true)]    // are equal
    [TestCase("2020.1.1f1", "2022.1.2", false)]     // is older
    [TestCase("2020.1.1f1", "2022.2", false)]       // is older
    [TestCase("2020.1.1f1", "2019.4", true)]        // is newer
    [TestCase("2021.1.1f1", "2020.3", true)]        // is newer
    public void IsNewerOrEqual(string currentUnityVersion, string versionToCheck, bool expected)
    {
        var application = new TestApplication(unityVersion: currentUnityVersion);

        var actual = SentryUnityVersion.IsNewerOrEqualThan(versionToCheck, application);

        Assert.AreEqual(expected, actual);
    }
}
