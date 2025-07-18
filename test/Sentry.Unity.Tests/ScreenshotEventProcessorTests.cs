using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    [Test]
    public void SentryEventProcessor_Process_CallsCaptureScreenshotForEvent()
    {
        var sentryMonoBehaviour = new TestSentryMonoBehaviour();

        var screenshotProcessor = new ScreenshotEventProcessor(new SentryUnityOptions(), sentryMonoBehaviour);
        screenshotProcessor.Process(new SentryEvent());

        Assert.IsTrue(sentryMonoBehaviour.CaptureScreenshotForEventCalled);
    }
}
