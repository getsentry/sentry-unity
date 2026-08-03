using System;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Unity;

internal class GameStatsMetricsIntegration : ISdkIntegration
{
    private readonly SentryMonoBehaviour _monoBehaviour;

    public GameStatsMetricsIntegration(SentryMonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public void Register(IHub hub, SentryOptions sentryOptions)
    {
        var options = (SentryUnityOptions)sentryOptions;
        if (!options.AutoGameStatsMetrics)
        {
            return;
        }

        if (!options.EnableMetrics)
        {
            options.DiagnosticLogger?.LogWarning(
                "Game-stats metrics are enabled but 'EnableMetrics' is disabled. No metrics will be collected.");
            return;
        }

        var monitor = new GameStatsMonitor(new GameMetricAttributes(), options.DiagnosticLogger);
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.GameStatsMetricSampleIntervalSeconds));
        _monoBehaviour.StartGameStatsMetrics(monitor, interval);
        options.DiagnosticLogger?.LogInfo("Game-stats metrics sampling every {0}s.",
            options.GameStatsMetricSampleIntervalSeconds);
    }
}
