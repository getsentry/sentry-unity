using System;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Unity.Metrics;

namespace Sentry.Unity.Integrations;

internal class GcMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public GcMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = sentryOptions as SentryUnityOptions
            ?? throw new ArgumentException("Options is not of type 'SentryUnityOptions'.");

        if (!options.AutoGcMetrics)
        {
            return;
        }

        var monitor = new GcMonitor(options);
        var interval = options.GcMetricsInterval < TimeSpan.FromSeconds(1)
            ? TimeSpan.FromSeconds(1)
            : options.GcMetricsInterval;
        _monoBehaviour.StartMetricsMonitor(monitor, interval);

        options.DiagnosticLogger?.LogInfo("GC metrics sampling every {0}s.", interval.TotalSeconds);
    }
}
