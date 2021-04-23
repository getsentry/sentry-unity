namespace Sentry.Unity
{
    internal interface IEventCapture
    {
        SentryId Capture(SentryEvent sentryEvent);
    }

    internal class EventCapture : IEventCapture
    {
        public SentryId Capture(SentryEvent sentryEvent)
            => SentrySdk.CaptureEvent(sentryEvent);
    }
}
