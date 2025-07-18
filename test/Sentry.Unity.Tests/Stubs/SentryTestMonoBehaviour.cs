using System;

namespace Sentry.Unity.Tests.Stubs;

internal class TestSentryMonoBehaviour : ISentryMonoBehaviour
{
    public event Action? ApplicationResuming;
    public bool CaptureScreenshotForEventCalled {get; private set;}

    public void ResumeApplication() => ApplicationResuming?.Invoke();
    public void CaptureScreenshotForEvent(SentryUnityOptions options, SentryId eventId)
        => CaptureScreenshotForEventCalled = true;
}
