using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

internal class LifeCycleIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _sentryMonoBehaviour;
    private readonly IApplication _application;

    public LifeCycleIntegration(SentryMonoBehaviour sentryMonoBehaviour, IApplication? application = null)
    {
        _sentryMonoBehaviour = sentryMonoBehaviour;
        _application = application ?? ApplicationAdapter.Instance;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        _sentryMonoBehaviour.ApplicationResuming += () => OnApplicationResuming(hub, options);
        _sentryMonoBehaviour.ApplicationPausing += () => OnApplicationPausing(hub, options);
        _application.Quitting += () => OnApplicationQuitting(hub, options);
    }

    private static void OnApplicationResuming(IHub hub, SentryOptions options)
    {
        if (!options.AutoSessionTracking)
        {
            return;
        }

        if (hub.IsSessionActive)
        {
            options.DiagnosticLogger?.LogDebug("Resuming session.");
            hub.ResumeSession();
        }
        else
        {
            options.DiagnosticLogger?.LogDebug("No active session to resume found. Starting a new session.");
            hub.StartSession();
        }
    }

    private static void OnApplicationPausing(IHub hub, SentryOptions options)
    {
        if (!options.AutoSessionTracking)
        {
            return;
        }

        if (hub.IsSessionActive)
        {
            options.DiagnosticLogger?.LogDebug("Pausing session.");
            hub.PauseSession();
        }
    }

    private static void OnApplicationQuitting(IHub hub, SentryOptions options)
    {
        options.DiagnosticLogger?.LogInfo("Quitting. Pausing session and flushing.");

        // Note: iOS applications are usually suspended and do not quit. You should tick "Exit on Suspend" in Player settings for iOS builds to cause the game to quit and not suspend, otherwise you may not see this call.
        //   If "Exit on Suspend" is not ticked then you will see calls to OnApplicationPause instead.
        // Note: On Windows Store Apps and Windows Phone 8.1 there is no application quit event. Consider using OnApplicationFocus event when focusStatus equals false.
        // Note: On WebGL it is not possible to implement OnApplicationQuit due to nature of the browser tabs closing.

        // 'OnQuitting' is invoked even when an uncaught exception happens in the ART. To make sure the .NET
        // SDK checks with the native layer on restart if the previous run crashed (through the CrashedLastRun callback)
        // we'll just pause sessions on shutdown. On restart, they can be closed with the right timestamp and as 'exited'.
        if (options.AutoSessionTracking && hub.IsSessionActive)
        {
            hub.PauseSession();
        }

        hub.FlushAsync(options.ShutdownTimeout).GetAwaiter().GetResult();
    }
}
