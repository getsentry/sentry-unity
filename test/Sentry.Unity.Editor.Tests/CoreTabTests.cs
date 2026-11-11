using NUnit.Framework;
using Sentry.Unity.Editor.ConfigurationWindow;

namespace Sentry.Unity.Editor.Tests;

public class CoreTabTests
{
    [Test]
    [TestCase(-1f, 0.01f)]
    [TestCase(0f, 0.01f)]
    [TestCase(0.005f, 0.01f)]
    [TestCase(0.01f, 0.01f)]
    [TestCase(0.5f, 0.5f)]
    [TestCase(1f, 1f)]
    [TestCase(2f, 1f)]
    public void ClampSampleRate_KeepsValueWithinSupportedRange(float sampleRate, float expected)
    {
        Assert.AreEqual(expected, CoreTab.ClampSampleRate(sampleRate));
    }

    [Test]
    public void ClampSampleRate_Zero_IsAcceptedByToSentryUnityOptions()
    {
        var scriptableOptions = UnityEngine.ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
        scriptableOptions.SampleRate = CoreTab.ClampSampleRate(0f);

        var options = scriptableOptions.ToSentryUnityOptions(isBuilding: true);

        Assert.AreEqual(CoreTab.MinSampleRate, options.SampleRate);
    }
}
