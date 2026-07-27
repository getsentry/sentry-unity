using System;
using System.Collections.Generic;
using Sentry.Extensibility;

namespace Sentry.Unity;

/// <summary>
/// Periodically samples the garbage collector and emits the number of collections per generation
/// since the previous sample as a counter metric.
/// <para>
/// Note: unlike the Unreal SDK's FSentryPerfGCMonitor, this reports collection <b>counts</b> rather
/// than pause <b>duration</b> - Unity's Mono/IL2CPP runtime does not expose a reliable GC pause
/// callback.
/// </para>
/// </summary>
internal class GcMonitor
{
    internal const string GcCollectionsMetric = "game.perf.gc_collections";

    private readonly GameMetricAttributes _attributes;
    private readonly IDiagnosticLogger? _logger;
    private readonly int[] _previousCounts;

    public GcMonitor(GameMetricAttributes attributes, IDiagnosticLogger? logger)
    {
        _attributes = attributes;
        _logger = logger;

        _previousCounts = new int[GC.MaxGeneration + 1];
        for (var generation = 0; generation < _previousCounts.Length; generation++)
        {
            _previousCounts[generation] = GC.CollectionCount(generation);
        }
    }

    public void Sample()
    {
        try
        {
            for (var generation = 0; generation < _previousCounts.Length; generation++)
            {
                var current = GC.CollectionCount(generation);
                var delta = current - _previousCounts[generation];
                _previousCounts[generation] = current;

                if (delta <= 0)
                {
                    continue;
                }

                // Attach the generation alongside the shared attributes.
                var attributes = new List<KeyValuePair<string, object>>(_attributes.Current)
                {
                    new("gc.generation", generation)
                };

                SentrySdk.Metrics.EmitCounter(GcCollectionsMetric, delta, attributes);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to emit GC metrics.");
        }
    }
}
