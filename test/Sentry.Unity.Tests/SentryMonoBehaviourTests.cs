using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class SentryMonoBehaviourTests
{
    private class Fixture
    {
        public SentryMonoBehaviour GetSut()
        {
            var gameObject = new GameObject("PauseTest");
            var sentryMonoBehaviour = gameObject.AddComponent<SentryMonoBehaviour>();
            sentryMonoBehaviour.Application = new TestApplication();

            return sentryMonoBehaviour;
        }
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [Test]
    public void OnApplicationPause_PauseStatusTrue_ApplicationPausingInvoked()
    {
        var wasPausingCalled = false;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => wasPausingCalled = true;

        sut.OnApplicationPause(true);

        Assert.IsTrue(wasPausingCalled);
    }

    [Test]
    public void OnApplicationFocus_FocusFalse_ApplicationPausingInvoked()
    {
        var wasPausingCalled = false;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => wasPausingCalled = true;

        sut.OnApplicationFocus(false);

        Assert.IsTrue(wasPausingCalled);
    }

    [Test]
    public void UpdatePauseStatus_PausedTwice_ApplicationPausingInvokedOnlyOnce()
    {
        var counter = 0;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => counter++;

        sut.UpdatePauseStatus(true);
        sut.UpdatePauseStatus(true);

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void UpdatePauseStatus_ResumedTwice_ApplicationResumingInvokedOnlyOnce()
    {
        var counter = 0;

        var sut = _fixture.GetSut();
        sut.ApplicationResuming += () => counter++;
        // We need to pause it first to resume it.
        sut.UpdatePauseStatus(true);

        sut.UpdatePauseStatus(false);
        sut.UpdatePauseStatus(false);

        Assert.AreEqual(1, counter);
    }
}