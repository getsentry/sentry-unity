using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class UnityWebGLExceptionHandlerTests
{
    private class Fixture
    {
        public TestHub Hub { get; set; } = null!;
        public SentryUnityOptions SentryOptions { get; set; } = null!;

        public UnityWebGLExceptionHandler GetSut()
        {
            var application = new TestApplication();
            var integration = new UnityWebGLExceptionHandler(application);
            integration.Register(Hub, SentryOptions);
            return integration;
        }
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture
        {
            Hub = new TestHub(),
            SentryOptions = new SentryUnityOptions()
        };
    }
    [Test]
    public void OnLogMessageReceived_LogTypeException_CaptureExceptionsEnabled_EventCaptured()
    {
        var sut = _fixture.GetSut();
        var message = TestContext.CurrentContext.Test.Name;

        sut.OnLogMessageReceived(message, "stacktrace", LogType.Exception);

        Assert.AreEqual(1, _fixture.Hub.CapturedEvents.Count);
    }
}
