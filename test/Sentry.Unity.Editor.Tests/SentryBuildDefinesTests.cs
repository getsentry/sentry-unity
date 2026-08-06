using NUnit.Framework;
using Sentry.Unity.Editor;

namespace Sentry.Unity.Editor.Tests;

public class SentryBuildDefinesTests
{
    [TestCase("")]
    [TestCase("FEATURE_A;FEATURE_B")]
    public void IsDisabled_DefineMissing_ReturnsFalse(string defines) =>
        Assert.False(SentryBuildDefines.IsDisabled(defines));

    [TestCase("SENTRY_UNITY_DISABLED")]
    [TestCase("FEATURE_A;SENTRY_UNITY_DISABLED;FEATURE_B")]
    public void IsDisabled_DefinePresent_ReturnsTrue(string defines) =>
        Assert.True(SentryBuildDefines.IsDisabled(defines));
}
