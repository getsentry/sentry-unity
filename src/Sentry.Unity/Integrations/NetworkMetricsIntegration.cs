using System;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity;

internal class NetworkMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public NetworkMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = (SentryUnityOptions)sentryOptions;
        if (!options.AutoNetworkMetrics)
        {
            return;
        }

        if (!options.EnableMetrics)
        {
            options.DiagnosticLogger?.LogWarning(
                "Network metrics are enabled but 'EnableMetrics' is disabled. No metrics will be collected.");
            return;
        }

        var monitor = new NetworkMetricsMonitor(new GameMetricAttributes(), options.DiagnosticLogger);
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.NetworkMetricsSampleIntervalSeconds));
        _monoBehaviour.StartNetworkMetrics(monitor.Sample, interval);
        options.DiagnosticLogger?.LogInfo("Network metrics sampling every {0}s.", options.NetworkMetricsSampleIntervalSeconds);
    }
}
