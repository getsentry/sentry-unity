using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Unity.Metrics;

namespace Sentry.Unity.Integrations;

internal class NetworkMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public NetworkMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = sentryOptions as SentryUnityOptions
            ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        if (!options.AutoNetworkMetrics)
        {
            return;
        }

        var monitor = new NetworkMetricsMonitor(options);
        if (!monitor.IsAvailable)
        {
            return;
        }

        var interval = options.NetworkMetricsInterval < TimeSpan.FromSeconds(1)
            ? TimeSpan.FromSeconds(1)
            : options.NetworkMetricsInterval;
        _monoBehaviour.StartMetricsMonitor(monitor, interval);

        options.DiagnosticLogger?.LogInfo("Network metrics sampling every {0}s.", interval.TotalSeconds);
    }
}
