using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Metrics;

/// <summary>
/// Samples per-frame performance and emits frame-time and FPS metrics every Nth frame, plus the
/// CPU main-/render-thread times when the engine's <see cref="FrameTimingManager"/> has data.
/// Driven once per frame from <see cref="SentryMonoBehaviour"/>.
/// </summary>
internal class FrameTimeMonitor : IGameMetricMonitor
{
    internal const string FrameTimeMetric = "game.perf.frame_time";
    internal const string FpsMetric = "game.perf.fps";
    internal const string GameThreadMetric = "game.perf.game_thread";
    internal const string RenderThreadMetric = "game.perf.render_thread";

    private readonly int _sampleInterval;
    private readonly GameMetricAttributes _attributes;
    private readonly IDiagnosticLogger? _logger;
    private readonly ISentryUnityInfo _unityInfo;

    private int _framesUntilSample;

    public FrameTimeMonitor(SentryUnityOptions options)
    {
        _sampleInterval = Math.Max(1, options.FrameMetricsSampleIntervalFrames);
        _framesUntilSample = _sampleInterval;
        _attributes = options.GameMetricAttributes;
        _logger = options.DiagnosticLogger;
        _unityInfo = options.UnityInfo;
    }

    /// <summary>
    /// Called once per frame on the main thread. Emits a sample every <c>sampleInterval</c> frames.
    /// </summary>
    public void Sample()
    {
        if (--_framesUntilSample > 0)
        {
            return;
        }

        _framesUntilSample = _sampleInterval;

        try
        {
            var deltaSeconds = Time.unscaledDeltaTime;
            var attributes = _attributes.Current;

            SentrySdk.Metrics.EmitDistribution(
                FrameTimeMetric, deltaSeconds * 1000.0, MeasurementUnit.Duration.Millisecond, attributes);
            SentrySdk.Metrics.EmitGauge(
                FpsMetric, deltaSeconds > 0f ? 1.0 / deltaSeconds : 0.0, MeasurementUnit.None, attributes);

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
