using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Samples per-frame performance and emits frame-time and FPS metrics every Nth frame, plus the
/// CPU main-/render-thread times when the engine's <see cref="FrameTimingManager"/> has data.
/// Driven once per frame from <see cref="SentryMonoBehaviour"/>. Mirrors the Unreal SDK's
/// FSentryPerfFrameTimeMonitor.
/// </summary>
internal class FrameTimeMonitor
{
    internal const string FrameTimeMetric = "game.perf.frame_time";
    internal const string FpsMetric = "game.perf.fps";
    internal const string GameThreadMetric = "game.perf.game_thread";
    internal const string RenderThreadMetric = "game.perf.render_thread";

    private readonly int _sampleInterval;
    private readonly GameMetricAttributes _attributes;
    private readonly IDiagnosticLogger? _logger;

    // Reused between samples to avoid a per-sample allocation.
    private readonly FrameTiming[] _timings = new FrameTiming[1];

    private int _frameCount;

    public FrameTimeMonitor(int sampleInterval, GameMetricAttributes attributes, IDiagnosticLogger? logger)
    {
        _sampleInterval = Math.Max(1, sampleInterval);
        _attributes = attributes;
        _logger = logger;
    }

    /// <summary>
    /// Called once per frame on the main thread. Emits a sample every <c>sampleInterval</c> frames.
    /// </summary>
    public void OnFrame()
    {
        if (++_frameCount % _sampleInterval != 0)
        {
            return;
        }

        try
        {
            var deltaSeconds = Time.unscaledDeltaTime;
            var attributes = _attributes.Current;

            SentrySdk.Metrics.EmitDistribution(
                FrameTimeMetric, deltaSeconds * 1000.0, MeasurementUnit.Duration.Millisecond, attributes);
            SentrySdk.Metrics.EmitGauge(
                FpsMetric, deltaSeconds > 0f ? 1.0 / deltaSeconds : 0.0, MeasurementUnit.None, attributes);

            EmitThreadTimings(attributes);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to emit frame-time metrics.");
        }
    }

    // CPU main-/render-thread times from the FrameTimingManager. These are already tracked by the
    // engine (no GPU timer queries are involved), so reading them adds no measurable overhead. GPU
    // timing is intentionally not read. The data is only available when 'Frame Timing Stats' is
    // enabled (implicit in development builds), so these metrics are emitted best-effort and are
    // silently skipped when unavailable.
    private void EmitThreadTimings(IReadOnlyList<KeyValuePair<string, object>> attributes)
    {
        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, _timings) == 0)
        {
            return;
        }

        var timing = _timings[0];

        if (timing.cpuMainThreadFrameTime > 0.0)
        {
            SentrySdk.Metrics.EmitDistribution(
                GameThreadMetric, timing.cpuMainThreadFrameTime, MeasurementUnit.Duration.Millisecond, attributes);
        }

        if (timing.cpuRenderThreadFrameTime > 0.0)
        {
            SentrySdk.Metrics.EmitDistribution(
                RenderThreadMetric, timing.cpuRenderThreadFrameTime, MeasurementUnit.Duration.Millisecond, attributes);
        }
    }
}
