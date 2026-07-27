using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Drives the sampling for the auto-collected game performance metrics. The per-frame coroutine
/// resumes every frame during the player loop, so <see cref="FrameTimeMonitor.OnFrame"/> observes
/// the current frame's <see cref="Time.unscaledDeltaTime"/> on the main thread. The periodic
/// coroutine drives the game-stats and GC monitors at a fixed wall-clock interval.
/// </summary>
public partial class SentryMonoBehaviour
{
    private Coroutine? _frameMetricsCoroutine;
    private Coroutine? _periodicMetricsCoroutine;
    private Coroutine? _networkMetricsCoroutine;

    internal void StartFrameTimeMetrics(FrameTimeMonitor monitor)
    {
        if (_frameMetricsCoroutine is not null)
        {
            StopCoroutine(_frameMetricsCoroutine);
        }

        _frameMetricsCoroutine = StartCoroutine(FrameMetricsCoroutine(monitor));
    }

    internal void StartPeriodicMetrics(Action sample, TimeSpan interval)
    {
        if (_periodicMetricsCoroutine is not null)
        {
            StopCoroutine(_periodicMetricsCoroutine);
        }

        _periodicMetricsCoroutine = StartCoroutine(PeriodicMetricsCoroutine(sample, interval));
    }

    internal void StartNetworkMetrics(Action sample, TimeSpan interval)
    {
        if (_networkMetricsCoroutine is not null)
        {
            StopCoroutine(_networkMetricsCoroutine);
        }

        _networkMetricsCoroutine = StartCoroutine(PeriodicMetricsCoroutine(sample, interval));
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
