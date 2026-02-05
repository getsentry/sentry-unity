using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

internal class LifeCycleIntegration : ISdkIntegration
{
    private static readonly Dictionary<string, string> ForegroundData = new() { { "state", "foreground" } };
    private static readonly Dictionary<string, string> BackgroundData = new() { { "state", "background" } };

    private IHub? _hub;
    private SentryUnityOptions _options = null!; // Set during register

    private readonly SentryMonoBehaviour _sentryMonoBehaviour;
    private readonly IApplication _application;

    public LifeCycleIntegration(SentryMonoBehaviour sentryMonoBehaviour, IApplication? application = null)
    {
        _application = application ?? ApplicationAdapter.Instance;
        _sentryMonoBehaviour = sentryMonoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        _hub = hub;
        // This should never happen, but if it does...
        _options = sentryOptions as SentryUnityOptions ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        if (!_options.AutoSessionTracking)
        {
            sentryOptions.LogDebug("AutoSessionTracking is disabled. Skipping {0}.", nameof(LifeCycleIntegration));
            return;
        }

        _sentryMonoBehaviour.ApplicationResuming += () =>
        {
            if (!hub.IsEnabled)
            {
                return;
            }

            hub.AddBreadcrumb(new Breadcrumb(
                type: "navigation",
                category: "app.lifecycle",
                data: ForegroundData,
                level: BreadcrumbLevel.Info));

            _options.LogDebug("Resuming session.");
            hub.ResumeSession();
        };

        _sentryMonoBehaviour.ApplicationPausing += () =>
        {
            if (!hub.IsEnabled)
            {
                return;
            }

            hub.AddBreadcrumb(new Breadcrumb(
                type: "navigation",
                category: "app.lifecycle",
                data: BackgroundData,
                level: BreadcrumbLevel.Info));

            _options.LogDebug("Pausing session.");
            hub.PauseSession();
        };

        _application.Quitting += OnQuitting;
    }

    private void OnQuitting()
    {
        // Platform-specific behavior notes:
        // - iOS: Applications are usually suspended and do not quit. If `Exit on Suspend` is enabled in Player Settings,
        //   the application will be terminated on suspend instead of calling this method. In that case,
        //   `OnApplicationPause` will be called instead.
        // - Windows Store Apps/Windows Phone 8.1: No application quit event exists. Use OnApplicationFocus instead.
        // - WebGL: OnApplicationQuit cannot be implemented due to browser tab closing behavior.

        // Session handling on shutdown:
        // This method is invoked even when an uncaught exception occurs (including crashes in native layers).
        // We pause the session here rather than ending it to ensure the .NET SDK can properly detect crashes
        // on the next startup (via the CrashedLastRun callback). The session will then be closed with the
        // correct timestamp during initialization.
        if (_options.AutoSessionTracking)
        {
            _hub?.PauseSession();
        }

        _hub?.FlushAsync(_options.ShutdownTimeout).GetAwaiter().GetResult();
    }
}
