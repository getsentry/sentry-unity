using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Unity.Metrics;

namespace Sentry.Unity.Integrations;

internal class FrameTimeMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public FrameTimeMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = sentryOptions as SentryUnityOptions
            ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        if (!options.AutoFrameMetrics)
        {
            return;
        }

        if (!options.EnableMetrics)
        {
            options.DiagnosticLogger?.LogWarning(
                "Frame-time metrics are enabled but 'EnableMetrics' is disabled. No metrics will be collected.");
            return;
        }

        var monitor = new FrameTimeMonitor(options);
        var interval = options.FrameMetricsInterval < TimeSpan.FromSeconds(1)
            ? TimeSpan.FromSeconds(1)
            : options.FrameMetricsInterval;
        _monoBehaviour.StartMetricsMonitor(monitor, interval);

        options.DiagnosticLogger?.LogInfo("Frame-time metrics enabled (sampling every {0}s).", interval.TotalSeconds);
    }
}
