using System;
using Sentry.Extensibility;
using UnityEngine.Profiling;

namespace Sentry.Unity;

/// <summary>
/// Periodically samples game statistics (memory usage) and emits them as gauge metrics. Mirrors the
/// Unreal SDK's FSentryPerfGameStatsMonitor (Unity has no UObject count, so managed/native heap
/// sizes are reported instead).
/// </summary>
internal class GameStatsMonitor
{
    internal const string UsedMemoryMetric = "game.perf.used_memory";
    internal const string ReservedMemoryMetric = "game.perf.reserved_memory";
    internal const string MonoUsedMemoryMetric = "game.perf.mono_used_memory";

    private readonly GameMetricAttributes _attributes;
    private readonly IDiagnosticLogger? _logger;

    public GameStatsMonitor(GameMetricAttributes attributes, IDiagnosticLogger? logger)
    {
        _attributes = attributes;
        _logger = logger;
    }

    public void Sample()
    {
        try
        {
            var attributes = _attributes.Current;

            // The Profiler returns '0' when a value is not available - skip those.
            var used = Profiler.GetTotalAllocatedMemoryLong();
            if (used > 0)
            {
                SentrySdk.Metrics.EmitGauge(UsedMemoryMetric, used, MeasurementUnit.Information.Byte, attributes);
            }

            var reserved = Profiler.GetTotalReservedMemoryLong();
            if (reserved > 0)
            {
                SentrySdk.Metrics.EmitGauge(ReservedMemoryMetric, reserved, MeasurementUnit.Information.Byte, attributes);
            }

            var monoUsed = Profiler.GetMonoUsedSizeLong();
            if (monoUsed > 0)
            {
                SentrySdk.Metrics.EmitGauge(MonoUsedMemoryMetric, monoUsed, MeasurementUnit.Information.Byte, attributes);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to emit game-stats metrics.");
        }
    }
}
