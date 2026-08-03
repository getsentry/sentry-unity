using System;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity;

internal class GcMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public GcMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = (SentryUnityOptions)sentryOptions;
        if (!options.AutoGcMetrics)
        {
            return;
        }

        if (!options.EnableMetrics)
        {
            options.DiagnosticLogger?.LogWarning(
                "GC metrics are enabled but 'EnableMetrics' is disabled. No metrics will be collected.");
            return;
        }

        var monitor = new GcMonitor(new GameMetricAttributes(), options.DiagnosticLogger);
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.GameStatsMetricSampleIntervalSeconds));
        _monoBehaviour.StartGcMetrics(monitor, interval);
        options.DiagnosticLogger?.LogInfo("GC metrics sampling every {0}s.", options.GameStatsMetricSampleIntervalSeconds);
    }
}
