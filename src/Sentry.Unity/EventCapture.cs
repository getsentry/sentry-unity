namespace Sentry.Unity;

internal interface IEventCapture
{
    SentryId Capture(SentryEvent sentryEvent);
}