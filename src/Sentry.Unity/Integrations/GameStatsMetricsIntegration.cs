using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Unity.Metrics;

namespace Sentry.Unity.Integrations;

internal class GameStatsMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public GameStatsMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = sentryOptions as SentryUnityOptions
            ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        if (!options.AutoMemoryMetrics)
        {
            return;
        }

        var monitor = new GameStatsMonitor(options);
        var interval = options.MemoryMetricsInterval < TimeSpan.FromSeconds(1)
            ? TimeSpan.FromSeconds(1)
            : options.MemoryMetricsInterval;
        _monoBehaviour.StartMetricsMonitor(monitor, interval);

        options.DiagnosticLogger?.LogInfo("Memory metrics sampling every {0}s.", interval.TotalSeconds);
    }
}
