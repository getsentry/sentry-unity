using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace Sentry.Unity.Editor.Tests;

public class NamedBuildTargetsTests
{
    [Test]
    [TestCase(StandaloneBuildSubtarget.Player, "Standalone")]
    [TestCase(StandaloneBuildSubtarget.Server, "Server")]
    public void From_Standalone_RespectsSubtarget(StandaloneBuildSubtarget subtarget, string expected)
    {
        var actual = NamedBuildTargets.From(BuildTargetGroup.Standalone, subtarget);

        Assert.AreEqual(expected, actual.TargetName);
    }
}
