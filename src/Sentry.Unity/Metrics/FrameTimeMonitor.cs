using System;
using System.Collections.Generic;
using System.Reflection;
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
    private readonly bool _supportsThreadTimings;

    // Reused between samples to avoid a per-sample allocation.
    private readonly FrameTiming[] _timings = new FrameTiming[1];

    // These fields are available starting with Unity 2022.3.
    private static readonly FieldInfo? CpuMainThreadFrameTime =
        typeof(FrameTiming).GetField("cpuMainThreadFrameTime");
    private static readonly FieldInfo? CpuRenderThreadFrameTime =
        typeof(FrameTiming).GetField("cpuRenderThreadFrameTime");

    private int _frameCount;

    public FrameTimeMonitor(int sampleInterval, GameMetricAttributes attributes, IDiagnosticLogger? logger)
    {
        _sampleInterval = Math.Max(1, sampleInterval);
        _attributes = attributes;
        _logger = logger;
        _supportsThreadTimings = SentryUnityVersion.IsNewerOrEqualThan("2022.3");
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

    // The per-thread fields are available in newer Unity versions only. Resolve them dynamically so
    // this precompiled assembly remains usable with Unity 2021.
    private void EmitThreadTimings(IReadOnlyList<KeyValuePair<string, object>> attributes)
    {
        if (!_supportsThreadTimings)
        {
            return;
        }

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, _timings) == 0)
        {
            return;
        }

        var timing = _timings[0];

        EmitThreadTiming(GameThreadMetric, CpuMainThreadFrameTime, timing, attributes);
        EmitThreadTiming(RenderThreadMetric, CpuRenderThreadFrameTime, timing, attributes);
    }

    private static void EmitThreadTiming(
        string metric,
        FieldInfo? timingField,
        FrameTiming timing,
        IReadOnlyList<KeyValuePair<string, object>> attributes)
    {
        if (timingField?.GetValue(timing) is double duration && duration > 0.0)
        {
            SentrySdk.Metrics.EmitDistribution(
                metric, duration, MeasurementUnit.Duration.Millisecond, attributes);
        }
    }
}
