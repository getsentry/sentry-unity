using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity;

internal class FrameTimeMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public FrameTimeMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = (SentryUnityOptions)sentryOptions;
        if (!options.AutoFrameTimeMetrics)
        {
            return;
        }

        if (!options.EnableMetrics)
        {
            options.DiagnosticLogger?.LogWarning(
                "Frame-time metrics are enabled but 'EnableMetrics' is disabled. No metrics will be collected.");
            return;
        }

        var monitor = new FrameTimeMonitor(
            options.FrameTimeMetricSampleInterval,
            new GameMetricAttributes(),
            options.DiagnosticLogger,
            options.UnityInfo);
        _monoBehaviour.StartFrameTimeMetrics(monitor);
        options.DiagnosticLogger?.LogInfo("Frame-time metrics enabled (sampling every {0} frames).",
            options.FrameTimeMetricSampleInterval);
    }
}
