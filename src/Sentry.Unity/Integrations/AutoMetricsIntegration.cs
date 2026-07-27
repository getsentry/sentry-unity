using System;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity;

/// <summary>
/// Registers the auto-collected game performance metrics (frame time, FPS, memory, GC). Metrics are
/// emitted through the managed metrics API (<see cref="SentrySdk.Metrics"/>) and sent to Sentry via
/// the regular transport, so no native support is required. Mirrors the Unreal SDK's
/// USentrySubsystem::ConfigurePerformanceMetrics.
/// </summary>
internal class AutoMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public AutoMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = (SentryUnityOptions)sentryOptions;

        var frameTime = options.AutoFrameTimeMetrics;
        var gameStats = options.AutoGameStatsMetrics;
        var gc = options.AutoGcMetrics;
        var network = options.AutoNetworkMetrics;

        // Nothing to collect.
        if (!frameTime && !gameStats && !gc && !network)
        {
            return;
        }

        // The managed metrics emitter is a no-op unless the metrics API is enabled.
        if (!options.EnableMetrics)
        {
            options.DiagnosticLogger?.LogWarning(
                "Auto game metrics are enabled but 'EnableMetrics' is disabled. No metrics will be collected.");
            return;
        }

        var logger = options.DiagnosticLogger;

        // Shared attributes (hardware info + current scene) attached to every metric.
        var attributes = new GameMetricAttributes();

        if (frameTime)
        {
            var monitor = new FrameTimeMonitor(options.FrameTimeMetricSampleInterval, attributes, logger);
            _monoBehaviour.StartFrameTimeMetrics(monitor);
            logger?.LogInfo("Frame-time metrics enabled (sampling every {0} frames).",
                options.FrameTimeMetricSampleInterval);
        }

        // Game-stats and GC are both periodic snapshots, so they share a single wall-clock ticker.
        Action? periodicSample = null;

        if (gameStats)
        {
            var monitor = new GameStatsMonitor(attributes, logger);
            periodicSample += monitor.Sample;
            logger?.LogInfo("Game-stats metrics enabled.");
        }

        if (gc)
        {
            var monitor = new GcMonitor(attributes, logger);
            periodicSample += monitor.Sample;
            logger?.LogInfo("GC metrics enabled.");
        }

        if (periodicSample is not null)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(1, options.GameStatsMetricSampleIntervalSeconds));
            _monoBehaviour.StartPeriodicMetrics(periodicSample, interval);
            logger?.LogInfo("Periodic game metrics sampling every {0}s.",
                options.GameStatsMetricSampleIntervalSeconds);
        }

        // Network is polled on its own (typically faster) ticker, matching the Unreal SDK.
        if (network)
        {
            var monitor = new NetworkMetricsMonitor(attributes, logger);
            var interval = TimeSpan.FromSeconds(Math.Max(1, options.NetworkMetricsSampleIntervalSeconds));
            _monoBehaviour.StartNetworkMetrics(monitor.Sample, interval);
            logger?.LogInfo("Network metrics sampling every {0}s.", options.NetworkMetricsSampleIntervalSeconds);
        }
    }
}
