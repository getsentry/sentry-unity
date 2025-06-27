using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessorWithHint
{
    private readonly SentryUnityOptions _options;
    private readonly IApplication _application;
    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions) : this(sentryOptions, null) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, IApplication? application)
    {
        _options = sentryOptions;
        _application = application ?? ApplicationAdapter.Instance;
    }

    public SentryEvent? Process(SentryEvent @event)
    {
        return @event;
    }

    public SentryEvent? Process(SentryEvent @event, SentryHint hint)
    {
        if (!MainThreadData.IsMainThread())
        {
            _options.DiagnosticLogger?.LogDebug("Screenshot capture skipped. Can't capture screenshots on other than the main thread.");
            return @event;
        }

        if (_options.BeforeCaptureScreenshotInternal?.Invoke() is not false)
        {
            if (_application.IsEditor)
            {
                _options.DiagnosticLogger?.LogInfo("Screenshot capture skipped. Capturing screenshots it not supported in the Editor");
                return @event;
            }

            if (Screen.width == 0 || Screen.height == 0)
            {
                _options.DiagnosticLogger?.LogWarning("Can't capture screenshots on a screen with a resolution of '{0}x{1}'.", Screen.width, Screen.height);
            }
            else
            {
                hint.AddAttachment(SentryScreenshotUtility.Capture(_options), "screenshot.jpg", contentType: "image/jpeg");
            }
        }
        else
        {
            _options.DiagnosticLogger?.LogInfo("Screenshot capture skipped by BeforeAttachScreenshot callback.");
        }

        return @event;
    }
}
