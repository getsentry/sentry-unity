using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Drives the sampling for the auto-collected game performance metrics. The per-frame coroutine
/// resumes every frame during the player loop, so <see cref="FrameTimeMonitor.OnFrame"/> observes
/// the current frame's <see cref="Time.unscaledDeltaTime"/> on the main thread. The periodic
/// coroutines drive periodic monitors at fixed wall-clock intervals.
/// </summary>
public partial class SentryMonoBehaviour
{
    private Coroutine? _frameMetricsCoroutine;
    private Coroutine? _gameStatsMetricsCoroutine;
    private Coroutine? _gcMetricsCoroutine;
    private Coroutine? _networkMetricsCoroutine;

    internal void StartFrameTimeMetrics(FrameTimeMonitor monitor)
    {
        if (_frameMetricsCoroutine is not null)
        {
            StopCoroutine(_frameMetricsCoroutine);
        }

        _frameMetricsCoroutine = StartCoroutine(FrameMetricsCoroutine(monitor));
    }

    internal void StartGameStatsMetrics(GameStatsMonitor monitor, TimeSpan interval)
    {
        StartPeriodicMetrics(monitor.Sample, interval, ref _gameStatsMetricsCoroutine);
    }

    internal void StartGcMetrics(GcMonitor monitor, TimeSpan interval)
    {
        StartPeriodicMetrics(monitor.Sample, interval, ref _gcMetricsCoroutine);
    }

    internal void StartNetworkMetrics(Action sample, TimeSpan interval)
    {
        if (_networkMetricsCoroutine is not null)
        {
            StopCoroutine(_networkMetricsCoroutine);
        }

        _networkMetricsCoroutine = StartCoroutine(PeriodicMetricsCoroutine(sample, interval));
    }

    private void StartPeriodicMetrics(Action sample, TimeSpan interval, ref Coroutine? coroutine)
    {
        if (coroutine is not null)
        {
            StopCoroutine(coroutine);
        }

        coroutine = StartCoroutine(PeriodicMetricsCoroutine(sample, interval));
    }

    private static IEnumerator FrameMetricsCoroutine(FrameTimeMonitor monitor)
    {
        // Skip the first frame - the synchronous startup stall would skew the very first sample.
        yield return null;

        while (true)
        {
            monitor.OnFrame();
            yield return null;
        }
    }

    private static IEnumerator PeriodicMetricsCoroutine(Action sample, TimeSpan interval)
    {
        var wait = new WaitForSecondsRealtime((float)interval.TotalSeconds);
        while (true)
        {
            yield return wait;
            sample();
        }
    }
}
