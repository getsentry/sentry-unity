using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessor
{
    private readonly SentryUnityOptions _options;
    private readonly SentryMonoBehaviour _sentryMonoBehaviour;

    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions) : this(sentryOptions, SentryMonoBehaviour.Instance) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, SentryMonoBehaviour? sentryMonoBehaviour)
    {
        _options = sentryOptions;
        _sentryMonoBehaviour = sentryMonoBehaviour ?? SentryMonoBehaviour.Instance;
    }

    public SentryEvent Process(SentryEvent @event)
    {
        _sentryMonoBehaviour.CaptureScreenshotForEvent(_options, @event.EventId);
        return @event;
    }
}
