using Sentry.Extensibility;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessor
{
    private readonly SentryUnityOptions _options;
    private readonly ISentryMonoBehaviour _sentryMonoBehaviour;

    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions) : this(sentryOptions, SentryMonoBehaviour.Instance) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, ISentryMonoBehaviour sentryMonoBehaviour)
    {
        _options = sentryOptions;
        _sentryMonoBehaviour = sentryMonoBehaviour;
    }

    public SentryEvent Process(SentryEvent @event)
    {
        // Screenshot capture is happening within the MonoBehaviour as it has to happen at the "EndOfFrame"
        _sentryMonoBehaviour.CaptureScreenshotForEvent(_options, @event.EventId);
        return @event;
    }
}
