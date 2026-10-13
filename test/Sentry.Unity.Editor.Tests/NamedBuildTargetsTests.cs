using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace Sentry.Unity.Editor.Tests;

public class NamedBuildTargetsTests
{
    [Test]
    [TestCase(BuildTargetGroup.Standalone, StandaloneBuildSubtarget.Player, "Standalone")]
    [TestCase(BuildTargetGroup.Standalone, StandaloneBuildSubtarget.Server, "Server")]
    [TestCase(BuildTargetGroup.Android, StandaloneBuildSubtarget.Server, "Android")]
    public void From_ReturnsExpectedNamedBuildTarget(BuildTargetGroup group, StandaloneBuildSubtarget subtarget, string expected)
    {
        var actual = NamedBuildTargets.From(group, subtarget);

        Assert.AreEqual(expected, actual.TargetName);
    }
}
