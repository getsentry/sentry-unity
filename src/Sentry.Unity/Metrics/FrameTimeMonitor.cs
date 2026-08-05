using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Metrics;

/// <summary>
/// Samples frame performance and emits frame-time and FPS metrics, plus the CPU main-/render-thread
/// times when the engine's <see cref="FrameTimingManager"/> has data.
/// </summary>
internal class FrameTimeMonitor : IGameMetricMonitor
{
    internal const string FrameTimeMetric = "game.perf.frame_time";
    internal const string FpsMetric = "game.perf.fps";
    internal const string GameThreadMetric = "game.perf.game_thread";
    internal const string RenderThreadMetric = "game.perf.render_thread";
    internal const string TargetFpsAttribute = "target_fps";
    internal const string VsyncCountAttribute = "vsync_count";

    private readonly GameMetricAttributes _attributes;
    private readonly IDiagnosticLogger? _logger;
    private readonly ISentryUnityInfo _unityInfo;
    private readonly List<KeyValuePair<string, object>> _frameAttributes = new(9);

    public FrameTimeMonitor(SentryUnityOptions options)
    {
        _attributes = options.GameMetricAttributes;
        _logger = options.DiagnosticLogger;
        _unityInfo = options.UnityInfo;
    }

    /// <summary>
    /// Called on the main thread at the configured realtime interval.
    /// </summary>
    public void Sample()
    {
        try
        {
            var deltaSeconds = Time.unscaledDeltaTime;
            var attributes = GetAttributes();

            SentrySdk.Metrics.EmitDistribution(
                FrameTimeMetric, deltaSeconds * 1000.0, MeasurementUnit.Duration.Millisecond, attributes);
            if (deltaSeconds > 0f)
            {
                SentrySdk.Metrics.EmitGauge(
                    FpsMetric, 1.0 / deltaSeconds, MeasurementUnit.None, attributes);
            }

            // Supported in Unity 2022 or newer
            if (_unityInfo.TryGetFrameThreadTimings(out var gameThreadTime, out var renderThreadTime))
            {
                EmitThreadTiming(GameThreadMetric, gameThreadTime, attributes);
                EmitThreadTiming(RenderThreadMetric, renderThreadTime, attributes);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to emit frame-time metrics.");
        }
    }

    private IReadOnlyList<KeyValuePair<string, object>> GetAttributes()
    {
        _frameAttributes.Clear();
        _frameAttributes.AddRange(_attributes.Current);
        _frameAttributes.Add(new KeyValuePair<string, object>(TargetFpsAttribute, Application.targetFrameRate));
        _frameAttributes.Add(new KeyValuePair<string, object>(VsyncCountAttribute, QualitySettings.vSyncCount));
        return _frameAttributes;
    }

    private static void EmitThreadTiming(
        string metric,
        double duration,
        IReadOnlyList<KeyValuePair<string, object>> attributes)
    {
        if (duration > 0.0)
        {
            SentrySdk.Metrics.EmitDistribution(
                metric, duration, MeasurementUnit.Duration.Millisecond, attributes);
        }
    }
}
